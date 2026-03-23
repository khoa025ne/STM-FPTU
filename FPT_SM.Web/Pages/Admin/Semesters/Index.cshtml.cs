using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Semesters;

public class IndexModel : BasePageModel
{
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public IndexModel(ISemesterService semesterService, IHubContext<NotificationHub> hubContext)
    {
        _semesterService = semesterService;
        _hubContext = hubContext;
    }

    public List<SemesterDto> Semesters { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        ViewData["Title"] = "Quản lý Học kỳ";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        Semesters = await _semesterService.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAdvanceStatusAsync(int id)
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        var result = await _semesterService.AdvanceStatusAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Cập nhật trạng thái học kỳ thành công!";
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _hubContext.Clients.Group(role).SendAsync("SemesterUpdated", new { action = "status", data = result.Data });
            }
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể thay đổi trạng thái.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        var result = await _semesterService.DeleteAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Đã xóa học kỳ.";
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _hubContext.Clients.Group(role).SendAsync("SemesterUpdated", new { action = "delete", id });
            }
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể xóa học kỳ.";
        }
        return RedirectToPage();
    }
}
