using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Admin;

public class DashboardModel : BasePageModel
{
    private readonly IDashboardService _dashboardService;
    public DashboardModel(IDashboardService dashboardService) { _dashboardService = dashboardService; }

    public AdminDashboardDto? DashboardData { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        ViewData["Title"] = "Dashboard";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        DashboardData = await _dashboardService.GetAdminDashboardAsync();
        return Page();
    }
}
