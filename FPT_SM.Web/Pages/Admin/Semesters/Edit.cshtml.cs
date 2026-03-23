using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Semesters;

public class EditModel : BasePageModel
{
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public EditModel(ISemesterService semesterService, IHubContext<NotificationHub> hubContext)
    {
        _semesterService = semesterService;
        _hubContext = hubContext;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public SemesterDto? Semester { get; set; }

    [BindProperty]
    public UpdateSemesterDto UpdateDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        Semester = await _semesterService.GetByIdAsync(Id);
        if (Semester == null)
        {
            TempData["Error"] = "Không tìm thấy học kỳ.";
            return RedirectToPage("/Admin/Semesters/Index");
        }

        ViewData["Title"] = $"Sửa: {Semester.Name}";

        UpdateDto = new UpdateSemesterDto
        {
            Name = Semester.Name,
            Code = Semester.Code,
            StartDate = Semester.StartDate,
            EndDate = Semester.EndDate
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        if (!ModelState.IsValid)
        {
            Semester = await _semesterService.GetByIdAsync(Id);
            ViewData["Title"] = $"Sửa: {Semester?.Name}";
            return Page();
        }

        var result = await _semesterService.UpdateAsync(Id, UpdateDto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi cập nhật.");
            Semester = await _semesterService.GetByIdAsync(Id);
            ViewData["Title"] = $"Sửa: {Semester?.Name}";
            return Page();
        }

        var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var role in roles)
        {
            await _hubContext.Clients.Group(role).SendAsync("SemesterUpdated", new
            {
                id = Id,
                action = "edit",
                name = UpdateDto.Name
            });
        }

        TempData["Success"] = $"Đã cập nhật học kỳ <b>{UpdateDto.Name}</b> thành công!";
        return RedirectToPage("/Admin/Semesters/Index");
    }
}
