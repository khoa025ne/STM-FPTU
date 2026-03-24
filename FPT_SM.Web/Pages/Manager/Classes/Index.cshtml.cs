using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Manager.Classes;

public class IndexModel : BasePageModel
{
    private readonly IClassService _classService;
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public IndexModel(
        IClassService classService,
        ISemesterService semesterService,
        IHubContext<NotificationHub> hubContext)
    {
        _classService = classService;
        _semesterService = semesterService;
        _hubContext = hubContext;
    }

    public List<ClassDto> Classes { get; set; } = new();
    public List<SemesterDto> Semesters { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? SemesterId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Manager");
        if (auth != null) return auth;

        ViewData["Title"] = "Quản lý Lớp học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "M";
        ViewData["UserId"] = CurrentUserId;

        // Load sequentially to avoid DbContext concurrency issues
        Semesters = await _semesterService.GetAllAsync();
        Classes = await _classService.GetAllAsync(SemesterId, null, Status);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var auth = RequireRole("Manager");
        if (auth != null) return auth;

        var result = await _classService.ApproveAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Đã phê duyệt lớp học.";
            var groups = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var group in groups)
            {
                await _hubContext.Clients.Group(group).SendAsync("ClassUpdated", new { action = "approve", data = result.Data });
                await _hubContext.Clients.Group(group).SendAsync("EntityChanged", new { entity = "Class", action = "approve", id, data = result.Data });
            }
            await _hubContext.Clients.Group($"class_{id}").SendAsync("ClassApproved", new { id, action = "approve" });
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể phê duyệt lớp học.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
    {
        var auth = RequireRole("Manager");
        if (auth != null) return auth;

        var result = await _classService.CancelAsync(id, reason);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Đã hủy lớp học.";
            var groups = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var group in groups)
            {
                await _hubContext.Clients.Group(group).SendAsync("ClassUpdated", new { action = "cancel", data = result.Data });
                await _hubContext.Clients.Group(group).SendAsync("EntityChanged", new { entity = "Class", action = "cancel", id, data = result.Data });
            }
            await _hubContext.Clients.Group($"class_{id}").SendAsync("ClassApproved", new { id, action = "cancel" });
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể hủy lớp học.";
        }
        return RedirectToPage();
    }
}
