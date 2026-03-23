using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Subjects;

public class IndexModel : BasePageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public IndexModel(ISubjectService subjectService, IHubContext<NotificationHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    public List<SubjectDto> Subjects { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        ViewData["Title"] = "Quản lý Môn học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        Subjects = await _subjectService.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        var result = await _subjectService.DeleteAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Đã xóa môn học.";
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _hubContext.Clients.Group(role).SendAsync("SubjectUpdated", new { action = "delete", id });
            }
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể xóa môn học.";
        }
        return RedirectToPage();
    }
}
