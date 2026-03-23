using FPT_SM.BLL.Services.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class SuggestionService : ISuggestionService
{
    private readonly IStudentContextService _studentContextService;

    public SuggestionService(IStudentContextService studentContextService)
    {
        _studentContextService = studentContextService;
    }

    public async Task<List<string>> GenerateSuggestionsAsync(int userId)
    {
        try
        {
            var context = await _studentContextService.GetStudentContextAsync(userId);
            var suggestions = new List<string>();

            // Rule 1: Low wallet balance
            if (context.WalletBalance < 100000)
            {
                suggestions.Add("💳 Nạp tiền vào ví để thanh toán các dịch vụ");
            }

            // Rule 2: Many absences - attendance concern
            if (context.TotalAbsences > 3)
            {
                suggestions.Add("📚 Cải thiện việc tham dự lớp - bạn đã vắng khá nhiều");
            }

            // Rule 3: Low GPA - academic performance concern
            if (context.AverageGPA.HasValue && context.AverageGPA < 2.0)
            {
                suggestions.Add("📈 Xem lại kế hoạch học tập để nâng điểm");
            }

            // Rule 4: Quiz coming soon
            var upcomingQuizzesToday = context.UpcomingQuizzes
                .Where(q => q.StartTime > DateTime.Now && q.StartTime <= DateTime.Now.AddDays(1))
                .ToList();

            if (upcomingQuizzesToday.Any())
            {
                suggestions.Add($"🎯 Bạn có {upcomingQuizzesToday.Count} quiz sắp diễn ra - hãy chuẩn bị");
            }

            // Rule 5: No recent grades
            if (!context.RecentGrades.Any())
            {
                suggestions.Add("📊 Xem điểm số của bạn trong học kỳ này");
            }

            // Rule 6: Multiple active classes
            if (context.ActiveClasses.Count > 4)
            {
                suggestions.Add("⏰ Quản lý thời gian học tập với nhiều môn học");
            }

            // Rule 7: High wallet balance - not critical but informative
            if (context.WalletBalance > 1000000 && !suggestions.Any(s => s.Contains("💳")))
            {
                suggestions.Add("🏦 Số dư ví của bạn khá cao - có ý định chi tiêu?");
            }

            // Rule 8: Good GPA - encouragement
            if (context.AverageGPA.HasValue && context.AverageGPA >= 3.5)
            {
                suggestions.Add("🌟 Giữ vững thành tích học tập ưu tú của bạn");
            }

            // Default suggestions if none triggered
            if (!suggestions.Any())
            {
                suggestions.Add("📋 Kiểm tra lịch học và deadline sắp tới");
                suggestions.Add("💬 Hỏi tôi về điều gì liên quan đến học tập");
            }

            // Ensure we have 3-5 suggestions
            return suggestions.Take(5).ToList();
        }
        catch
        {
            // Return generic suggestions on error
            return new List<string>
            {
                "📋 Xem lịch học của bạn",
                "📊 Kiểm tra điểm số",
                "🏦 Quản lý ví tiền",
                "🎯 Xem quiz sắp tới",
                "💬 Hỏi tôi bất cứ điều gì"
            };
        }
    }
}
