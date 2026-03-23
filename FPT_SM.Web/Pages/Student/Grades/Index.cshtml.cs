using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Grades;

public class IndexModel : BasePageModel
{
    private readonly IGradeService _gradeService;

    public IndexModel(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    public List<GradeDto> Grades { get; set; } = new();
    public List<AcademicAnalysisDto> Analyses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Điểm số";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;

        Grades = await _gradeService.GetByStudentAsync(CurrentUserId!.Value);
        Analyses = await _gradeService.GetStudentAnalysesAsync(CurrentUserId!.Value);

        return Page();
    }
}
