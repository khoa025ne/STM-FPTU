using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Admin.Reports;

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

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    public List<SemesterDto> Semesters { get; set; } = new();
    public AdminOverviewDto Overview { get; set; } = new();
    public RevenueReportDto RevenueReport { get; set; } = new();
    public List<GradeReportDto> GradeReports { get; set; } = new();
    public List<EnrollmentReportDto> EnrollmentReports { get; set; } = new();
    public List<EnrollmentReportDto> TopSubjects { get; set; } = new();
    public List<SemesterEnrollmentDto> SemesterTrends { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        Semesters = await _semesterService.GetAllAsync();
        SemesterId ??= Semesters.FirstOrDefault(s => s.Status == 2)?.Id ?? Semesters.FirstOrDefault()?.Id;
        Year ??= DateTime.Now.Year;
        Month ??= DateTime.Now.Month;

        var filter = BuildFilter();
        Overview = await _reportService.GetAdminOverviewAsync();
        RevenueReport = await _reportService.GetRevenueReportAsync(filter);
        GradeReports = await _reportService.GetGradeReportAsync(filter);
        EnrollmentReports = await _reportService.GetEnrollmentReportAsync(filter);
        TopSubjects = EnrollmentReports.OrderByDescending(e => e.TotalEnrolled).Take(5).ToList();
        SemesterTrends = await _reportService.GetEnrollmentTrendAsync(null);

        SetViewData();
        return Page();
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var bytes = await _reportService.ExportGradeReportToExcelAsync(BuildFilter());
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCaoTongHop.xlsx");
    }

    public async Task<IActionResult> OnPostExportPdfAsync()
    {
        var bytes = await _reportService.ExportGradeReportToPdfAsync(BuildFilter());
        return File(bytes, "application/pdf", "BaoCaoTongHop.pdf");
    }

    private ReportFilterDto BuildFilter()
        => new()
        {
            SemesterId = SemesterId,
            Month = Month,
            Year = Year
        };

    private void SetViewData()
    {
        ViewData["Title"] = "Báo cáo tổng quan";
        ViewData["FullName"] = CurrentFullName;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;
        ViewData["UserInitial"] = string.IsNullOrEmpty(CurrentFullName) ? "A" : CurrentFullName[0].ToString();
        ViewData["UserId"] = CurrentUserId;
        ViewData["RollNumber"] = HttpContext.Session.GetString("RollNumber");
    }
}
