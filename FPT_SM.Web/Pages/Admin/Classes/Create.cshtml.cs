using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Classes;

public class CreateModel : BasePageModel
{
    private readonly IClassService _classService;
    private readonly ISubjectService _subjectService;
    private readonly IUserService _userService;
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreateModel(
        IClassService classService,
        ISubjectService subjectService,
        IUserService userService,
        ISemesterService semesterService,
        IHubContext<NotificationHub> hubContext)
    {
        _classService = classService;
        _subjectService = subjectService;
        _userService = userService;
        _semesterService = semesterService;
        _hubContext = hubContext;
    }

    [BindProperty]
    public CreateClassDto CreateDto { get; set; } = new();

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<UserDto> Teachers { get; set; } = new();
    public List<SlotDto> Slots { get; set; } = new();
    public List<SemesterDto> Semesters { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Tạo lớp học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        // Execute queries sequentially to avoid DbContext concurrency issues
        Subjects = await _subjectService.GetAllAsync();
        Teachers = await _userService.GetByRoleAsync(3); // RoleId 3 = Teacher
        Slots = await _classService.GetAllSlotsAsync();
        Semesters = await _semesterService.GetAllAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Tạo lớp học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        if (!ModelState.IsValid)
        {
            // Load data sequentially on validation error
            Subjects = await _subjectService.GetAllAsync();
            Teachers = await _userService.GetByRoleAsync(3);
            Slots = await _classService.GetAllSlotsAsync();
            Semesters = await _semesterService.GetAllAsync();

            return Page();
        }

        var result = await _classService.CreateAsync(CreateDto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi tạo lớp học.");

            // Load data sequentially on service error
            Subjects = await _subjectService.GetAllAsync();
            Teachers = await _userService.GetByRoleAsync(3);
            Slots = await _classService.GetAllSlotsAsync();
            Semesters = await _semesterService.GetAllAsync();

            return Page();
        }

        await _hubContext.Clients.Group("admin_all").SendAsync("ClassUpdated", new
        {
            id = result.Data?.Id ?? 0,
            action = "create",
            classCode = result.Data?.Name
        });

        await _hubContext.Clients.Group("manager_all").SendAsync("ClassUpdated", new
        {
            id = result.Data?.Id ?? 0,
            action = "create",
            classCode = result.Data?.Name
        });

        TempData["Success"] = $"Đã tạo lớp học <b>{CreateDto.Name}</b> thành công!";
        return RedirectToPage("/Admin/Classes/Index");
    }
}
