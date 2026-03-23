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
    public int? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ComparisonMonth { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ComparisonYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ComparisonYearForYearComparison { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool EnableComparison { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public string ComparisonMode { get; set; } = "month"; // "month" or "year"

    public List<SemesterDto> Semesters { get; set; } = new();
    public AdminOverviewDto Overview { get; set; } = new();
    public RevenueReportDto RevenueReport { get; set; } = new();
    public RevenueReportDto RevenueComparison { get; set; } = new();
    public List<GradeReportDto> GradeReports { get; set; } = new();
    public List<EnrollmentReportDto> EnrollmentReports { get; set; } = new();
    public List<EnrollmentReportDto> TopSubjects { get; set; } = new();
    public List<SemesterEnrollmentDto> SemesterTrends { get; set; } = new();
    
    // Comparison data
    public decimal RevenueChangePercent { get; set; } = 0;
    public decimal EnrollmentChangePercent { get; set; } = 0;
    public string CurrentPeriodName { get; set; } = "";
    public string ComparisonPeriodName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        Semesters = await _semesterService.GetAllAsync();
        Year ??= DateTime.Now.Year;
        Month ??= DateTime.Now.Month;
        ComparisonMode ??= "month";

        // Set default comparison values based on mode
        if (ComparisonMode == "month")
        {
            ComparisonMonth ??= DateTime.Now.Month > 1 ? DateTime.Now.Month - 1 : 12;
            ComparisonYear ??= DateTime.Now.Month > 1 ? DateTime.Now.Year : DateTime.Now.Year - 1;
            
            CurrentPeriodName = $"Tháng {Month} năm {Year}";
            ComparisonPeriodName = $"Tháng {ComparisonMonth} năm {ComparisonYear}";
        }
        else
        {
            ComparisonYearForYearComparison ??= Year > 2024 ? Year - 1 : 2024;
            CurrentPeriodName = $"Năm {Year}";
            ComparisonPeriodName = $"Năm {ComparisonYearForYearComparison}";
        }

        var filter = BuildFilter();
        Overview = await _reportService.GetAdminOverviewAsync();
        RevenueReport = await _reportService.GetRevenueReportAsync(filter);
        GradeReports = await _reportService.GetGradeReportAsync(filter);
        EnrollmentReports = await _reportService.GetEnrollmentReportAsync(filter);
        TopSubjects = EnrollmentReports.OrderByDescending(e => e.TotalEnrolled).Take(5).ToList();
        SemesterTrends = await _reportService.GetEnrollmentTrendAsync(null);

        // Get comparison data
        if (EnableComparison)
        {
            ReportFilterDto comparisonFilter;
            
            if (ComparisonMode == "year" && ComparisonYearForYearComparison.HasValue)
            {
                // Year-to-year comparison: get all months of comparison year
                comparisonFilter = new ReportFilterDto
                {
                    SemesterId = null,
                    Month = null, // All months
                    Year = ComparisonYearForYearComparison
                };
            }
            else
            {
                // Month-to-month comparison
                comparisonFilter = new ReportFilterDto
                {
                    SemesterId = null,
                    Month = ComparisonMonth,
                    Year = ComparisonYear
                };
            }
            
            RevenueComparison = await _reportService.GetRevenueReportAsync(comparisonFilter);
            
            // Calculate percent changes
            var currentRevenue = RevenueReport.Monthly.Sum(m => m.Revenue);
            var previousRevenue = RevenueComparison.Monthly.Sum(m => m.Revenue);
            if (previousRevenue > 0)
                RevenueChangePercent = ((currentRevenue - previousRevenue) / previousRevenue) * 100;

            var currentEnrollment = EnrollmentReports.Sum(e => e.TotalEnrolled);
            var previousEnrollment = await _reportService.GetEnrollmentReportAsync(comparisonFilter);
            if (previousEnrollment.Count > 0)
            {
                var prevEnrollmentTotal = previousEnrollment.Sum(e => e.TotalEnrolled);
                if (prevEnrollmentTotal > 0)
                    EnrollmentChangePercent = ((currentEnrollment - prevEnrollmentTotal) / prevEnrollmentTotal) * 100;
            }
        }

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
            SemesterId = null,
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
