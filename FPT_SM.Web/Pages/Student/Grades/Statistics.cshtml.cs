using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Grades;

public class StatisticsModel : BasePageModel
{
    private readonly IGradeService _gradeService;
    private readonly IReportService _reportService;
    private readonly ISemesterService _semesterService;

    public StatisticsModel(
        IGradeService gradeService,
        IReportService reportService,
        ISemesterService semesterService)
    {
        _gradeService = gradeService;
        _reportService = reportService;
        _semesterService = semesterService;
    }

    public List<GradeDto> AllGrades { get; set; } = new();
    public List<SemesterDto> AllSemesters { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Thống kê học tập";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;

        var userId = CurrentUserId!.Value;

        AllGrades = await _gradeService.GetByStudentAsync(userId);
        AllSemesters = await _semesterService.GetAllAsync();

        return Page();
    }

    /// <summary>
    /// Get GPA statistics per semester
    /// </summary>
    public List<(string semesterName, decimal gpa, int passed, int failed)> GetSemesterStats()
    {
        var stats = new List<(string, decimal, int, int)>();

        var gradesBySemester = AllGrades
            .Where(g => g.Status == 2 && g.GPA.HasValue) // Only published grades with GPA
            .GroupBy(g => g.SemesterId)
            .OrderBy(g => g.Key);

        foreach (var semGroup in gradesBySemester)
        {
            var semester = AllSemesters.FirstOrDefault(s => s.Id == semGroup.Key);
            var semesterName = semester?.Name ?? $"Kỳ {semGroup.Key}";

            var grades = semGroup.ToList();
            var avgGpa = grades.Count > 0 ? (decimal)Math.Round(grades.Average(g => (double)(g.GPA ?? 0)), 2) : 0;
            var passed = grades.Count(g => g.IsPassed);
            var failed = grades.Count - passed;

            stats.Add((semesterName, avgGpa, passed, failed));
        }

        return stats;
    }

    /// <summary>
    /// Get overall statistics
    /// </summary>
    public (decimal overallGpa, int totalPassed, int totalFailed, int totalCredits, double progressPercent) GetOverallStats()
    {
        var publishedGrades = AllGrades.Where(g => g.Status == 2 && g.GPA.HasValue).ToList();

        var overallGpa = publishedGrades.Count > 0
            ? (decimal)Math.Round(publishedGrades.Average(g => (double)(g.GPA ?? 0)), 2)
            : 0;

        var totalPassed = publishedGrades.Count(g => g.IsPassed);
        var totalFailed = publishedGrades.Count - totalPassed;

        // Assuming 3 credits per subject (FPT standard)
        var totalCredits = publishedGrades.Count * 3;

        // Progress: total subjects / max subjects per program (estimate: 40 subjects for 4-year program)
        var progressPercent = (publishedGrades.Count / 40.0) * 100;
        progressPercent = Math.Min(progressPercent, 100);

        return (overallGpa, totalPassed, totalFailed, totalCredits, progressPercent);
    }

    /// <summary>
    /// Get grades comparison with class average
    /// </summary>
    public (decimal studentGpa, decimal classAverage, string comparison) GetGpaComparison()
    {
        var publishedGrades = AllGrades.Where(g => g.Status == 2 && g.GPA.HasValue).ToList();
        var studentGpa = publishedGrades.Count > 0
            ? (decimal)Math.Round(publishedGrades.Average(g => (double)(g.GPA ?? 0)), 2)
            : 0;

        // Class average is calculated from all grades in the system for the same semesters
        // For demo purposes, we'll use a fixed class average
        var classAverage = 6.5m;

        var comparison = studentGpa > classAverage ? "Cao hơn" : studentGpa < classAverage ? "Thấp hơn" : "Bằng";

        return (studentGpa, classAverage, comparison);
    }

    public async Task<IActionResult> OnPostExportPdfAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        var userId = CurrentUserId!.Value;
        var filter = new ReportFilterDto { };

        try
        {
            var bytes = await _reportService.ExportGradeReportToPdfAsync(filter);
            return File(bytes, "application/pdf", $"ThongKeHocTap_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi xuất PDF: {ex.Message}";
            return RedirectToPage();
        }
    }
}
