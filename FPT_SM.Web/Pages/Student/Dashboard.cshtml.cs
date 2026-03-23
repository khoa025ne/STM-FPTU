using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student;

public class DashboardModel : BasePageModel
{
    private readonly IDashboardService _dashboardService;
    public DashboardModel(IDashboardService dashboardService) { _dashboardService = dashboardService; }

    public StudentDashboardDto? DashboardData { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Dashboard";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;

        DashboardData = await _dashboardService.GetStudentDashboardAsync(CurrentUserId!.Value);

        // Set RollNumber from first enrollment if available
        var rollNumber = DashboardData?.CurrentEnrollments?.FirstOrDefault()?.StudentRollNumber;
        if (!string.IsNullOrEmpty(rollNumber))
            ViewData["RollNumber"] = rollNumber;

        return Page();
    }
}
