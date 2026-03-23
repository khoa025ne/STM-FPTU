using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Teacher.Quizzes;

public class DetailsModel : BasePageModel
{
    private readonly IQuizService _quizService;
    private readonly IClassService _classService;

    public DetailsModel(IQuizService quizService, IClassService classService)
    {
        _quizService = quizService;
        _classService = classService;
    }

    public QuizDto? Quiz { get; set; }
    public List<QuizSubmissionReportDto> SubmissionReports { get; set; } = new();
    public int TotalStudents { get; set; }
    public int SubmittedCount { get; set; }
    public int NotSubmittedCount { get; set; }
    public bool CanExportReport { get; set; }
    public string ExportMessage { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public int QuizId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ClassId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        ViewData["Title"] = "Chi tiết Quiz";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "T";
        ViewData["UserId"] = CurrentUserId;

        // Get quiz info
        var quizzes = await _quizService.GetByClassAsync(ClassId);
        Quiz = quizzes.FirstOrDefault(q => q.Id == QuizId);
        if (Quiz == null) return NotFound();

        // Get submission reports
        SubmissionReports = await _quizService.GetSubmissionsReportAsync(QuizId, ClassId);

        TotalStudents = SubmissionReports.Count;
        SubmittedCount = SubmissionReports.Count(r => r.Status == "Submitted" || r.Status == "Graded");
        NotSubmittedCount = TotalStudents - SubmittedCount;

        // Check if can export (quiz must be ended)
        CanExportReport = DateTime.Now > Quiz.EndTime;
        if (!CanExportReport)
        {
            ExportMessage = $"Bài quiz sẽ kết thúc lúc {Quiz.EndTime:dd/MM/yyyy HH:mm}. Bạn sẽ có thể xuất báo cáo sau thời gian này.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        // Get quiz
        var quizzes = await _quizService.GetByClassAsync(ClassId);
        var quiz = quizzes.FirstOrDefault(q => q.Id == QuizId);
        if (quiz == null) return NotFound();

        // Check if quiz is ended
        if (DateTime.Now <= quiz.EndTime)
        {
            return new JsonResult(new
            {
                success = false,
                message = $"Chưa hết giờ làm bài. Bài quiz sẽ kết thúc lúc {quiz.EndTime:dd/MM/yyyy HH:mm}."
            });
        }

        // Get reports
        var reports = await _quizService.GetSubmissionsReportAsync(QuizId, ClassId);

        // Generate CSV
        var csv = GenerateCsvReport(reports, quiz.Title);
        var fileName = $"Quiz_{quiz.Title.Replace(" ", "_")}_{DateTime.Now:yyyy-MM-dd}.csv";

        return File(csv, "text/csv; charset=utf-8", fileName);
    }

    private byte[] GenerateCsvReport(List<QuizSubmissionReportDto> reports, string quizTitle)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("Mã số sinh viên,Tên sinh viên,Điểm,Số lần làm,Giờ làm,Giờ nộp");

        // Data
        foreach (var report in reports)
        {
            var startTime = report.StartedAt != DateTime.MinValue ? report.StartedAt.ToString("dd/MM/yyyy HH:mm") : "";
            var submitTime = report.SubmittedAt.HasValue ? report.SubmittedAt.Value.ToString("dd/MM/yyyy HH:mm") : "";
            var status = report.Status == "NotStarted" ? "Chưa làm" : "";

            sb.AppendLine($"{EscapeCsv(report.StudentCode)},{EscapeCsv(report.StudentName)},{report.Score},{report.AttemptCount},{startTime},{submitTime}{(string.IsNullOrEmpty(status) ? "" : $" ({status})")}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
