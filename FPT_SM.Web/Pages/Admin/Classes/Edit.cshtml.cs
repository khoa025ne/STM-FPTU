using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Classes;

public class EditModel : BasePageModel
{
    private readonly IClassService _classService;
    private readonly ISubjectService _subjectService;
    private readonly IUserService _userService;
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public EditModel(
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

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public ClassDto? Class { get; set; }

    [BindProperty]
    public UpdateClassDto UpdateDto { get; set; } = new();

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<UserDto> Teachers { get; set; } = new();
    public List<SlotDto> Slots { get; set; } = new();
    public List<SemesterDto> Semesters { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        Class = await _classService.GetByIdAsync(Id);
        if (Class == null)
        {
            TempData["Error"] = "Không tìm thấy lớp học.";
            return RedirectToPage("/Admin/Classes/Index");
        }

        ViewData["Title"] = $"Sửa: {Class.Name}";

        // Check if class status is Open (1)
        if (Class.Status != 1)
        {
            ViewData["Warning"] = "Chỉ có thể sửa lớp học ở trạng thái Open";
        }

        UpdateDto = new UpdateClassDto
        {
            TeacherId = Class.TeacherId,
            SlotId = Class.SlotId,
            Capacity = Class.Capacity
        };

        // Load data sequentially to avoid DbContext concurrency issues
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

        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        Class = await _classService.GetByIdAsync(Id);
        if (Class == null)
        {
            TempData["Error"] = "Không tìm thấy lớp học.";
            return RedirectToPage("/Admin/Classes/Index");
        }

        ViewData["Title"] = $"Sửa: {Class.Name}";

        // Check if class status is Open (1)
        if (Class.Status != 1)
        {
            ViewData["Warning"] = "Chỉ có thể sửa lớp học ở trạng thái Open";
            ModelState.AddModelError(string.Empty, "Chỉ có thể sửa lớp học ở trạng thái Open");

            // Load data sequentially
            Subjects = await _subjectService.GetAllAsync();
            Teachers = await _userService.GetByRoleAsync(3);
            Slots = await _classService.GetAllSlotsAsync();
            Semesters = await _semesterService.GetAllAsync();

            return Page();
        }

        if (!ModelState.IsValid)
        {
            // Load data sequentially
            Subjects = await _subjectService.GetAllAsync();
            Teachers = await _userService.GetByRoleAsync(3);
            Slots = await _classService.GetAllSlotsAsync();
            Semesters = await _semesterService.GetAllAsync();

            return Page();
        }

        var result = await _classService.UpdateAsync(Id, UpdateDto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi cập nhật.");

            // Load data sequentially
            Subjects = await _subjectService.GetAllAsync();
            Teachers = await _userService.GetByRoleAsync(3);
            Slots = await _classService.GetAllSlotsAsync();
            Semesters = await _semesterService.GetAllAsync();

            return Page();
        }

        var groups = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var group in groups)
        {
            await _hubContext.Clients.Group(group).SendAsync("ClassUpdated", new
            {
                id = Id,
                action = "edit",
                classCode = Class.Name
            });

            await _hubContext.Clients.Group(group).SendAsync("EntityChanged", new
            {
                entity = "Class",
                action = "edit",
                id = Id,
                classCode = Class.Name
            });
        }

        TempData["Success"] = $"Đã cập nhật lớp học <b>{Class.Name}</b> thành công!";
        return RedirectToPage("/Admin/Classes/Index");
    }
}
