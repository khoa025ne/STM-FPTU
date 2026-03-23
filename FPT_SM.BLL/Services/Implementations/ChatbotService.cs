using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace FPT_SM.BLL.Services.Implementations;

public class ChatbotService : IChatbotService
{
    private readonly IChatLogRepository _chatLogRepo;
    private readonly IGeminiService _geminiService;
    private readonly IStudentContextService _studentContextService;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        IChatLogRepository chatLogRepo,
        IGeminiService geminiService,
        IStudentContextService studentContextService,
        ILogger<ChatbotService> logger)
    {
        _chatLogRepo = chatLogRepo;
        _geminiService = geminiService;
        _studentContextService = studentContextService;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(int userId, string message)
    {
        string aiResponse;
        try
        {
            // Fetch student context for personalization
            var context = await _studentContextService.GetStudentContextAsync(userId);

            // Fetch recent conversation history for context memory
            var conversationHistory = await GetRecentConversationHistoryAsync(userId, limit: 5);

            // Build system prompt with context and conversation memory
            var systemPrompt = BuildSystemPrompt(context, conversationHistory);

            _logger.LogInformation($"[ChatBot] Processing message for user {userId}: {message.Substring(0, Math.Min(50, message.Length))}...");
            aiResponse = await _geminiService.GenerateContentAsync(systemPrompt, message);
            _logger.LogInformation($"[ChatBot] Got response from Gemini for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ChatBot] ❌ Error processing message for user {userId}: {ex.Message}\n{ex.StackTrace}");
            aiResponse = "Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này. Vui lòng thử lại sau.";
        }

        var chatLog = new ChatLog
        {
            UserId = userId,
            Message = message,
            Response = aiResponse,
            CreatedAt = DateTime.Now
        };
        await _chatLogRepo.AddAsync(chatLog);

        return aiResponse;
    }

    private async Task<List<(string Question, string Answer)>> GetRecentConversationHistoryAsync(int userId, int limit = 5)
    {
        try
        {
            var logs = await _chatLogRepo.GetByUserAsync(userId, limit: limit * 2);
            var history = new List<(string, string)>();

            // Get last 'limit' exchanges (most recent first, then reverse to chronological order)
            var recentLogs = logs.OrderByDescending(l => l.CreatedAt).Take(limit).OrderBy(l => l.CreatedAt);

            foreach (var log in recentLogs)
            {
                history.Add((log.Message, log.Response));
            }

            return history.Select(h => (h.Item1, h.Item2)).ToList();
        }
        catch
        {
            return new List<(string, string)>();
        }
    }

    private string BuildSystemPrompt(StudentContextDto context, List<(string Question, string Answer)> conversationHistory)
    {
        // Format active classes
        var classesStr = context.ActiveClasses.Any()
            ? string.Join(", ", context.ActiveClasses.Select(c => $"{c.ClassName} ({c.SubjectName})"))
            : "Không có lớp nào";

        // Format recent grades
        var gradesStr = context.RecentGrades.Any()
            ? string.Join("\n", context.RecentGrades.Select(g =>
                $"- {g.SubjectName}: GPA {g.GPA?.ToString("F2") ?? "N/A"}"))
            : "Chưa có điểm";

        // Format upcoming quizzes
        var quizzesStr = context.UpcomingQuizzes.Any()
            ? string.Join("\n", context.UpcomingQuizzes.Take(3).Select(q =>
                $"- {q.Title} ({q.ClassName}): {q.StartTime:dd/MM/yyyy HH:mm}"))
            : "Không có quiz sắp tới";

        // Format conversation history (NEW - for memory)
        var historyStr = "";
        if (conversationHistory.Any())
        {
            historyStr = "\nCỤC BỘ QUY CHIẾU CUỘC HỘI THOẠI TRƯỚC:\n";
            for (int i = 0; i < conversationHistory.Count; i++)
            {
                historyStr += $"Q{i + 1}: {conversationHistory[i].Question}\n";
                historyStr += $"A{i + 1}: {conversationHistory[i].Answer}\n";
            }
            historyStr += "\nHãy xem xét lịch sử cuộc hộp thoại để trả lời câu hỏi tiếp theo một cách phù hợp.\n";
        }

        return $@"Bạn là trợ lý AI cá nhân cho sinh viên {context.FullName} ({context.RollNumber}).

THÔNG TIN SINH VIÊN HIỆN TẠI:
- Học kỳ: {context.CurrentSemester}
- Lớp học hiện tại: {classesStr}
- Số lần vắng: {context.TotalAbsences} lớp
- Số dư ví: {context.WalletBalance:N0} VND
- Điểm trung bình (GPA): {context.AverageGPA?.ToString("F2") ?? "N/A"}

ĐIỂM SỐ GẦN ĐÂY:
{gradesStr}

QUIZ SẮP TỚI:
{quizzesStr}
{historyStr}
HƯỚNG DẪN TRẢ LỜI:
1. Khi sinh viên hỏi về thông tin cá nhân (điểm, ví, vắng, quiz), hãy dùng DỮ LIỆU THỰC ở trên để trả lời CỤ THỂ
2. Không chỉ hướng dẫn 'cách xem', mà hãy cho biết THÔNG TIN/ KẾT QUẢ HIỆN TẠI
3. Đưa lời khuyên phù hợp với tình trạng học tập của sinh viên
4. Luôn trả lời bằng tiếng Việt 100%
5. Thân thiện, tích cực, hỗ trợ
6. Hãy DỰA VÀO LỊ SỬ CUỘC HỘI THOẠI để cung cấp các câu trả lời nhất quán và liên kết giữa các câu hỏi

VÍ DỤ TRẢ LỜI TỐT:
- Hỏi: 'Mình vắng bao lớp?'
- Trả lời: 'Bạn hiện đã vắng {context.TotalAbsences} lớp. Hãy cố gắng đến đầy đủ vì vắng quá nhiều sẽ ảnh hưởng đến điểm số nhé!'

- Hỏi: 'Mình có quiz sắp tới không?'
- Trả lời: 'Vâng, bạn có {context.UpcomingQuizzes.Count} quiz sắp tới. [liệt kê chi tiết]'

- Hỏi: 'Điểm em bao nhiêu?'
- Trả lời: 'Dưới đây là điểm gần đây của bạn: [liệt kê cụ thể]. GPA trung bình: {context.AverageGPA?.ToString("F2")}'";
    }

    public async Task<List<ChatMessageDto>> GetHistoryAsync(int userId, int limit = 50)
    {
        var logs = await _chatLogRepo.GetByUserAsync(userId, limit);
        var result = new List<ChatMessageDto>();

        foreach (var log in logs.OrderBy(l => l.CreatedAt))
        {
            result.Add(new ChatMessageDto
            {
                Message = log.Message,
                Response = "",
                CreatedAt = log.CreatedAt,
                IsUser = true
            });
            result.Add(new ChatMessageDto
            {
                Message = "",
                Response = log.Response,
                CreatedAt = log.CreatedAt,
                IsUser = false
            });
        }

        return result;
    }
}
