using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Manager.Reports;

public class IndexModel : BasePageModel
{
    private readonly IReportService _reportService;
    private readonly ISemesterService _semesterService;

    public IndexModel(IReportService reportService, ISemesterService semesterService)
    {
        _reportService = reportService;
        _semesterService = semesterService;
    }

    [BindProperty(SupportsGet = true)]
    public int? SemesterId { get; set; }

    public List<SemesterDto> Semesters { get; set; } = new();
    public List<ManagerClassStatusDto> ClassStatuses { get; set; } = new();
    public List<ManagerTeacherReportDto> TeacherReports { get; set; } = new();
    public List<ClassExportDto> ClassList { get; set; } = new();
    public double ApprovalRate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Manager");
        if (redirect != null) return redirect;

        Semesters = await _semesterService.GetAllAsync();
        SemesterId ??= Semesters.FirstOrDefault(s => s.Status == 2)?.Id ?? Semesters.FirstOrDefault()?.Id;

        ClassStatuses = await _reportService.GetClassStatusReportAsync();
        TeacherReports = await _reportService.GetTeacherWorkloadAsync();
        ClassList = await _reportService.GetClassListAsync(new ReportFilterDto { SemesterId = SemesterId });

        var pending = ClassStatuses.FirstOrDefault(c => c.Status == 1)?.Count ?? 0;
        var active = ClassStatuses.FirstOrDefault(c => c.Status == 2)?.Count ?? 0;
        ApprovalRate = pending + active > 0 ? Math.Round((double)active / (pending + active) * 100, 1) : 0;

        SetViewData();
        return Page();
    }

    public async Task<IActionResult> OnPostExportClassesAsync()
    {
        var bytes = await _reportService.ExportClassReportToExcelAsync(new ReportFilterDto { SemesterId = SemesterId });
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachLop.xlsx");
    }

    private void SetViewData()
    {
        ViewData["Title"] = "Báo cáo quản lý";
        ViewData["FullName"] = CurrentFullName;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;
        ViewData["UserInitial"] = string.IsNullOrEmpty(CurrentFullName) ? "M" : CurrentFullName[0].ToString();
        ViewData["UserId"] = CurrentUserId;
        ViewData["RollNumber"] = HttpContext.Session.GetString("RollNumber");
    }
}
