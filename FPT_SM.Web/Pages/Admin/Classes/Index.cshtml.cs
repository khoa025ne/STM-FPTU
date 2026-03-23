using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Classes;

public class IndexModel : BasePageModel
{
    private readonly IClassService _classService;
    private readonly ISemesterService _semesterService;
    private readonly ISubjectService _subjectService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public IndexModel(
        IClassService classService,
        ISemesterService semesterService,
        ISubjectService subjectService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext)
    {
        _classService = classService;
        _semesterService = semesterService;
        _subjectService = subjectService;
        _userService = userService;
        _hubContext = hubContext;
    }

    public List<ClassDto> Classes { get; set; } = new();
    public List<SemesterDto> Semesters { get; set; } = new();
    public List<SubjectDto> Subjects { get; set; } = new();
    public List<UserDto> Teachers { get; set; } = new();
    public List<SlotDto> Slots { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? SemesterId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        ViewData["Title"] = "Quản lý Lớp học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        // Execute queries sequentially to avoid DbContext concurrency issues
        Classes = await _classService.GetAllAsync(SemesterId, SubjectId, Status);
        Semesters = await _semesterService.GetAllAsync();
        Subjects = await _subjectService.GetAllAsync();
        Teachers = await _userService.GetByRoleAsync(3); // RoleId 3 = Teacher
        Slots = await _classService.GetAllSlotsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var auth = RequireRole("Admin");
        if (auth != null) return auth;

        var result = await _classService.DeleteAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Đã xóa lớp học.";
            await _hubContext.Clients.Group("admin_all").SendAsync("ClassUpdated", new { action = "delete", id });
            await _hubContext.Clients.Group("manager_all").SendAsync("ClassUpdated", new { action = "delete", id });
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể xóa lớp học.";
        }
        return RedirectToPage();
    }
}
