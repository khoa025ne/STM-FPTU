using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Manager;

public class DashboardModel : BasePageModel
{
    private readonly IDashboardService _dashboardService;
    public DashboardModel(IDashboardService dashboardService) { _dashboardService = dashboardService; }

    public ManagerDashboardDto? DashboardData { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Manager");
        if (auth != null) return auth;

        ViewData["Title"] = "Dashboard";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "M";
        ViewData["UserId"] = CurrentUserId;

        DashboardData = await _dashboardService.GetManagerDashboardAsync();
        return Page();
    }
}
