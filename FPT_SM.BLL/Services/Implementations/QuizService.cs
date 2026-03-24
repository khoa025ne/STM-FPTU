using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FPT_SM.BLL.Services.Implementations;

public class QuizService : IQuizService
{
    private readonly IQuizRepository _quizRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly AppDbContext _context;
    private readonly IGeminiService _geminiService;

    public QuizService(IQuizRepository quizRepo, IEnrollmentRepository enrollmentRepo, AppDbContext context, IGeminiService geminiService)
    {
        _quizRepo = quizRepo;
        _enrollmentRepo = enrollmentRepo;
        _context = context;
        _geminiService = geminiService;
    }

    public async Task<List<QuizDto>> GetByClassAsync(int classId)
    {
        var quizzes = await _quizRepo.GetByClassAsync(classId);
        return quizzes.Select(q => MapToDto(q)).ToList();
    }

    public async Task<List<QuizDto>> GetForStudentAsync(int studentId, int? semesterId = null)
    {
        var quizzes = await _quizRepo.GetActiveForStudentAsync(studentId);
        if (semesterId.HasValue)
            quizzes = quizzes.Where(q => q.Class?.SemesterId == semesterId.Value);

        var result = new List<QuizDto>();
        foreach (var q in quizzes)
        {
            var submission = await _quizRepo.GetSubmissionAsync(q.Id, studentId);
            var dto = MapToDto(q);
            dto.HasSubmitted = submission?.Status == "Submitted" || submission?.Status == "Graded";
            dto.StudentScore = submission?.TotalScore;
            result.Add(dto);
        }
        return result;
    }

    public async Task<QuizDetailDto?> GetDetailAsync(int quizId, int? studentId = null)
    {
        var quiz = await _quizRepo.GetWithQuestionsAsync(quizId);
        if (quiz == null) return null;

        var dto = new QuizDetailDto
        {
            Id = quiz.Id,
            ClassId = quiz.ClassId,
            ClassName = quiz.Class?.Name ?? "",
            SubjectName = quiz.Class?.Subject?.Name ?? "",
            Title = quiz.Title,
            Description = quiz.Description,
            StartTime = quiz.StartTime,
            EndTime = quiz.EndTime,
            DurationMinutes = quiz.DurationMinutes,
            IsPublishResult = quiz.IsPublishResult,
            QuizType = quiz.QuizType,
            Status = quiz.Status,
            QuestionCount = quiz.Questions?.Count ?? 0,
            Questions = quiz.Questions?.OrderBy(q => q.OrderIndex).Select(q => new QuizQuestionDto
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Points = q.Points,
                OrderIndex = q.OrderIndex,
                Options = q.AnswerOptions?.OrderBy(o => o.OrderIndex).Select(o => new QuizAnswerOptionDto
                {
                    Id = o.Id,
                    OptionText = o.OptionText,
                    IsCorrect = studentId == null && o.IsCorrect, // hide answer for students
                    OrderIndex = o.OrderIndex
                }).ToList() ?? new()
            }).ToList() ?? new()
        };

        if (studentId.HasValue)
        {
            var submission = await _quizRepo.GetSubmissionAsync(quizId, studentId.Value);
            dto.HasSubmitted = submission?.Status == "Submitted" || submission?.Status == "Graded";
            dto.StudentScore = submission?.TotalScore;
        }

        return dto;
    }

    public async Task<ServiceResult<QuizDto>> CreateAsync(CreateQuizDto dto)
    {
        if (dto.EndTime <= dto.StartTime)
            return ServiceResult<QuizDto>.Failure("Thời gian kết thúc phải sau thời gian bắt đầu.");

        // Validate total points must equal 10
        var totalPoints = dto.Questions.Sum(q => q.Points);
        if (totalPoints != 10)
            return ServiceResult<QuizDto>.Failure($"Tổng điểm bài quiz phải bằng 10. Hiện tại: {totalPoints} điểm. Vui lòng điều chỉnh điểm số các câu hỏi.");

        var quiz = new Quiz
        {
            ClassId = dto.ClassId,
            Title = dto.Title,
            Description = dto.Description,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            DurationMinutes = dto.DurationMinutes,
            IsPublishResult = dto.IsPublishResult,
            QuizType = dto.QuizType,
            Status = dto.Status,
            CreatedAt = DateTime.Now
        };

        await _quizRepo.AddAsync(quiz);

        // Add questions
        foreach (var qDto in dto.Questions.OrderBy(q => q.OrderIndex))
        {
            var question = new QuizQuestion
            {
                QuizId = quiz.Id,
                QuestionText = qDto.QuestionText,
                QuestionType = qDto.QuestionType,
                Points = qDto.Points,
                OrderIndex = qDto.OrderIndex
            };
            _context.QuizQuestions.Add(question);
            await _context.SaveChangesAsync();

            foreach (var optDto in qDto.Options.OrderBy(o => o.OrderIndex))
            {
                _context.QuizAnswerOptions.Add(new QuizAnswerOption
                {
                    QuestionId = question.Id,
                    OptionText = optDto.OptionText,
                    IsCorrect = optDto.IsCorrect,
                    OrderIndex = optDto.OrderIndex
                });
            }
            await _context.SaveChangesAsync();
        }

        var created = await _quizRepo.GetWithQuestionsAsync(quiz.Id);
        return ServiceResult<QuizDto>.Success(MapToDto(created!), "Tạo quiz thành công!");
    }

    public async Task<ServiceResult<GeneratedQuizDraftDto>> GenerateDraftFromTextAsync(GenerateQuizFromTextDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SourceText))
            return ServiceResult<GeneratedQuizDraftDto>.Failure("Nguồn dữ liệu text không được để trống.");

        var questionCount = Math.Clamp(dto.QuestionCount, 5, 50);
        var normalizedText = dto.SourceText.Trim();
        if (normalizedText.Length > 60000)
            normalizedText = normalizedText[..60000];

        var systemPrompt =
            "Bạn là trợ lý tạo quiz cho giáo viên. Nhiệm vụ: từ tài liệu text đầu vào, tạo bộ câu hỏi trắc nghiệm một đáp án đúng. " +
            "BẮT BUỘC trả về JSON hợp lệ, không markdown, không giải thích ngoài JSON.";

                var userPrompt = $@"Hãy tạo một quiz theo yêu cầu:
- Số câu: {questionCount}
- Độ khó: {dto.Difficulty ?? "mixed"}
- Tiêu đề gợi ý: {dto.QuizTitle ?? "Quiz từ tài liệu"}

Format JSON phải theo schema:
{{
    ""title"": ""string"",
    ""description"": ""string"",
    ""questions"": [
        {{
            ""questionText"": ""string"",
            ""options"": [""A"", ""B"", ""C"", ""D""],
            ""correctIndex"": 0
        }}
    ]
}}

Ràng buộc:
- Mỗi câu đúng 4 đáp án.
- correctIndex thuộc [0..3].
- Câu hỏi bám sát nội dung tài liệu.

Dữ liệu tài liệu:
{normalizedText}";

        string raw;
        try
        {
            // ONE-CALL mode: exactly one Gemini API request.
            raw = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);
        }
        catch (Exception ex)
        {
            return ServiceResult<GeneratedQuizDraftDto>.Failure($"AI không phản hồi: {ex.Message}");
        }

        var cleanedJson = raw.Trim();
        if (cleanedJson.StartsWith("```"))
        {
            cleanedJson = cleanedJson
                .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty)
                .Trim();
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(cleanedJson);
        }
        catch
        {
            return ServiceResult<GeneratedQuizDraftDto>.Failure("AI trả về không đúng định dạng JSON. Vui lòng thử lại.");
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("questions", out var questionsEl) || questionsEl.ValueKind != JsonValueKind.Array)
                return ServiceResult<GeneratedQuizDraftDto>.Failure("AI chưa sinh được danh sách câu hỏi hợp lệ.");

            var questions = new List<CreateQuizQuestionDto>();
            var idx = 0;
            foreach (var q in questionsEl.EnumerateArray())
            {
                if (idx >= questionCount) break;

                var qText = q.TryGetProperty("questionText", out var qTextEl)
                    ? (qTextEl.GetString() ?? string.Empty).Trim()
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(qText))
                    continue;

                if (!q.TryGetProperty("options", out var optsEl) || optsEl.ValueKind != JsonValueKind.Array)
                    continue;

                var options = optsEl.EnumerateArray()
                    .Select(x => (x.GetString() ?? string.Empty).Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(4)
                    .ToList();

                if (options.Count < 4)
                    continue;

                var correctIndex = q.TryGetProperty("correctIndex", out var correctEl) && correctEl.ValueKind == JsonValueKind.Number
                    ? correctEl.GetInt32()
                    : 0;

                if (correctIndex < 0 || correctIndex > 3)
                    correctIndex = 0;

                questions.Add(new CreateQuizQuestionDto
                {
                    QuestionText = qText,
                    QuestionType = "SingleChoice",
                    OrderIndex = idx,
                    Options = options.Select((opt, oi) => new CreateAnswerOptionDto
                    {
                        OptionText = opt,
                        IsCorrect = oi == correctIndex,
                        OrderIndex = oi
                    }).ToList()
                });

                idx++;
            }

            if (!questions.Any())
                return ServiceResult<GeneratedQuizDraftDto>.Failure("Không tạo được câu hỏi hợp lệ từ dữ liệu input.");

            var perQuestion = Math.Round(10m / questions.Count, 2);
            var running = 0m;
            for (var i = 0; i < questions.Count; i++)
            {
                if (i == questions.Count - 1)
                {
                    questions[i].Points = Math.Round(10m - running, 2);
                }
                else
                {
                    questions[i].Points = perQuestion;
                    running += perQuestion;
                }
            }

            var title = doc.RootElement.TryGetProperty("title", out var titleEl)
                ? (titleEl.GetString() ?? string.Empty).Trim()
                : string.Empty;

            var description = doc.RootElement.TryGetProperty("description", out var descEl)
                ? (descEl.GetString() ?? string.Empty).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(title))
                title = dto.QuizTitle?.Trim() ?? $"Quiz tự động ({questions.Count} câu)";

            if (string.IsNullOrWhiteSpace(description))
                description = "Bộ câu hỏi được tạo tự động từ dữ liệu text bằng AI.";

            return ServiceResult<GeneratedQuizDraftDto>.Success(new GeneratedQuizDraftDto
            {
                Title = title,
                Description = description,
                Questions = questions
            }, $"AI đã tạo nháp quiz {questions.Count} câu hỏi.");
        }
    }

    public async Task<ServiceResult<QuizDto>> PublishAsync(int quizId)
    {
        var quiz = await _quizRepo.GetByIdAsync(quizId);
        if (quiz == null) return ServiceResult<QuizDto>.Failure("Không tìm thấy quiz.");
        if (quiz.Status == 2) return ServiceResult<QuizDto>.Failure("Quiz đã được công bố.");

        quiz.Status = 2;
        await _quizRepo.UpdateAsync(quiz);

        var updated = await _quizRepo.GetWithQuestionsAsync(quizId);
        return ServiceResult<QuizDto>.Success(MapToDto(updated!), "Công bố quiz thành công!");
    }

    public async Task<ServiceResult<int>> StartAttemptAsync(int quizId, int studentId)
    {
        var quiz = await _quizRepo.GetByIdAsync(quizId);
        if (quiz == null) return ServiceResult<int>.Failure("Không tìm thấy quiz.");
        if (quiz.Status != 2) return ServiceResult<int>.Failure("Quiz chưa được công bố.");
        if (DateTime.Now < quiz.StartTime) return ServiceResult<int>.Failure("Quiz chưa bắt đầu.");
        if (DateTime.Now > quiz.EndTime) return ServiceResult<int>.Failure("Quiz đã kết thúc.");

        var existing = await _quizRepo.GetSubmissionAsync(quizId, studentId);
        if (existing != null)
        {
            if (existing.Status == "InProgress") return ServiceResult<int>.Success(existing.Id, "Tiếp tục làm bài.");
            return ServiceResult<int>.Failure("Bạn đã nộp bài quiz này rồi.");
        }

        var submission = new QuizSubmission
        {
            QuizId = quizId,
            StudentId = studentId,
            StartedAt = DateTime.Now,
            Status = "InProgress",
            TotalScore = 0
        };
        await _quizRepo.AddSubmissionAsync(submission);
        return ServiceResult<int>.Success(submission.Id, "Bắt đầu làm bài thành công!");
    }

    public async Task<ServiceResult<QuizResultDto>> SubmitAsync(SubmitQuizDto dto, int studentId)
    {
        var submission = await _quizRepo.GetSubmissionWithAnswersAsync(dto.SubmissionId);
        if (submission == null) return ServiceResult<QuizResultDto>.Failure("Không tìm thấy bài làm.");
        if (submission.StudentId != studentId) return ServiceResult<QuizResultDto>.Failure("Không có quyền truy cập.");
        if (submission.Status != "InProgress") return ServiceResult<QuizResultDto>.Failure("Bài đã được nộp.");

        var quiz = await _quizRepo.GetWithQuestionsAsync(submission.QuizId);
        if (quiz == null) return ServiceResult<QuizResultDto>.Failure("Không tìm thấy quiz.");

        decimal totalScore = 0;
        var answerResults = new List<QuizAnswerResultDto>();

        foreach (var q in quiz.Questions)
        {
            var studentAnswer = dto.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            decimal scoreEarned = 0;
            bool? isCorrect = null;
            string? studentAnswerText = null;

            if (studentAnswer != null)
            {
                if (q.QuestionType == "Essay")
                {
                    studentAnswerText = studentAnswer.AnswerText;
                    // Essay grading manual
                }
                else
                {
                    var correctOptionIds = q.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                    var selectedIds = studentAnswer.SelectedOptionIds.ToHashSet();
                    isCorrect = correctOptionIds.SetEquals(selectedIds);
                    if (isCorrect == true) scoreEarned = q.Points;
                    studentAnswerText = string.Join(",", selectedIds);
                }

                _context.QuizStudentAnswers.Add(new QuizStudentAnswer
                {
                    SubmissionId = dto.SubmissionId,
                    QuestionId = q.Id,
                    AnswerText = studentAnswer.AnswerText,
                    SelectedOptionIds = studentAnswerText,
                    IsCorrect = isCorrect,
                    ScoreEarned = scoreEarned
                });
            }

            totalScore += scoreEarned;
            answerResults.Add(new QuizAnswerResultDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Points = q.Points,
                ScoreEarned = scoreEarned,
                IsCorrect = isCorrect,
                StudentAnswer = studentAnswerText,
                Options = q.AnswerOptions.Select(o => new QuizAnswerOptionDto
                {
                    Id = o.Id,
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex
                }).ToList()
            });
        }

        submission.TotalScore = totalScore;
        submission.SubmittedAt = DateTime.Now;
        submission.Status = "Submitted";
        await _quizRepo.UpdateSubmissionAsync(submission);
        await _context.SaveChangesAsync();

        var maxScore = quiz.Questions.Sum(q => q.Points);

        return ServiceResult<QuizResultDto>.Success(new QuizResultDto
        {
            SubmissionId = dto.SubmissionId,
            QuizTitle = quiz.Title,
            TotalScore = totalScore,
            MaxScore = maxScore,
            Status = "Submitted",
            SubmittedAt = submission.SubmittedAt,
            AnswerResults = answerResults
        }, "Nộp bài thành công!");
    }

    public async Task<QuizResultDto?> GetResultAsync(int submissionId, int studentId)
    {
        var submission = await _quizRepo.GetSubmissionWithAnswersAsync(submissionId);
        if (submission == null || submission.StudentId != studentId) return null;

        var quiz = await _quizRepo.GetWithQuestionsAsync(submission.QuizId);
        if (quiz == null) return null;

        var answerResults = quiz.Questions.Select(q =>
        {
            var sa = submission.StudentAnswers.FirstOrDefault(a => a.QuestionId == q.Id);
            return new QuizAnswerResultDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Points = q.Points,
                ScoreEarned = sa?.ScoreEarned ?? 0,
                IsCorrect = sa?.IsCorrect,
                StudentAnswer = sa?.SelectedOptionIds ?? sa?.AnswerText,
                Options = quiz.IsPublishResult
                    ? q.AnswerOptions.Select(o => new QuizAnswerOptionDto
                    {
                        Id = o.Id, OptionText = o.OptionText, IsCorrect = o.IsCorrect, OrderIndex = o.OrderIndex
                    }).ToList()
                    : new()
            };
        }).ToList();

        return new QuizResultDto
        {
            SubmissionId = submissionId,
            QuizTitle = quiz.Title,
            TotalScore = submission.TotalScore,
            MaxScore = quiz.Questions.Sum(q => q.Points),
            Status = submission.Status,
            SubmittedAt = submission.SubmittedAt,
            AnswerResults = answerResults
        };
    }

    public async Task<ServiceResult> DeleteAsync(int quizId)
    {
        var quiz = await _quizRepo.GetByIdAsync(quizId);
        if (quiz == null) return ServiceResult.Failure("Không tìm thấy quiz.");
        if (quiz.Status == 2) return ServiceResult.Failure("Không thể xóa quiz đã công bố.");

        await _quizRepo.DeleteAsync(quiz);
        return ServiceResult.Success("Xóa quiz thành công!");
    }

    public async Task<List<QuizSubmissionReportDto>> GetSubmissionsReportAsync(int quizId, int classId)
    {
        var quiz = await _quizRepo.GetByIdAsync(quizId);
        if (quiz == null) return new();

        // Get all enrollments in class with status 2 (Paid)
        var enrollments = await _enrollmentRepo.GetByClassAsync(classId);
        var activeEnrollments = enrollments.Where(e => e.Status == 2).ToList();

        var report = new List<QuizSubmissionReportDto>();

        foreach (var enrollment in activeEnrollments)
        {
            var submissions = await _context.QuizSubmissions
                .Where(s => s.QuizId == quizId && s.StudentId == enrollment.StudentId)
                .OrderBy(s => s.StartedAt)
                .ToListAsync();

            if (submissions.Any())
            {
                var submitted = submissions.Where(s => s.Status == "Submitted" || s.Status == "Graded").ToList();
                var lastSubmission = submitted.LastOrDefault() ?? submissions.Last();

                report.Add(new QuizSubmissionReportDto
                {
                    SubmissionId = lastSubmission.Id,
                    StudentCode = enrollment.Student?.RollNumber ?? "",
                    StudentName = enrollment.Student?.FullName ?? "",
                    Score = lastSubmission.TotalScore,
                    AttemptCount = submissions.Count,
                    StartedAt = submissions.First().StartedAt,
                    SubmittedAt = lastSubmission.SubmittedAt,
                    Status = lastSubmission.Status
                });
            }
            else
            {
                report.Add(new QuizSubmissionReportDto
                {
                    SubmissionId = 0,
                    StudentCode = enrollment.Student?.RollNumber ?? "",
                    StudentName = enrollment.Student?.FullName ?? "",
                    Score = 0,
                    AttemptCount = 0,
                    StartedAt = DateTime.MinValue,
                    SubmittedAt = null,
                    Status = "NotStarted"
                });
            }
        }

        return report.OrderBy(r => r.StudentCode).ToList();
    }

    private QuizDto MapToDto(Quiz q) => new()
    {
        Id = q.Id,
        ClassId = q.ClassId,
        ClassName = q.Class?.Name ?? "",
        SubjectName = q.Class?.Subject?.Name ?? "",
        Title = q.Title,
        Description = q.Description,
        StartTime = q.StartTime,
        EndTime = q.EndTime,
        DurationMinutes = q.DurationMinutes,
        IsPublishResult = q.IsPublishResult,
        QuizType = q.QuizType,
        Status = q.Status,
        QuestionCount = q.Questions?.Count ?? 0
    };
}
