using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Subjects;

public class EditModel : BasePageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public EditModel(ISubjectService subjectService, IHubContext<NotificationHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public SubjectDto? Subject { get; set; }

    public List<SubjectDto> AllSubjects { get; set; } = new();

    [BindProperty]
    public UpdateSubjectDto UpdateDto { get; set; } = new();

    [BindProperty]
    public string? PrerequisiteIds { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        Subject = await _subjectService.GetByIdAsync(Id);
        if (Subject == null)
        {
            TempData["Error"] = "Không tìm thấy môn học.";
            return RedirectToPage("/Admin/Subjects/Index");
        }

        ViewData["Title"] = $"Sửa: {Subject.Name}";

        AllSubjects = await _subjectService.GetAllAsync();

        UpdateDto = new UpdateSubjectDto
        {
            Code = Subject.Code,
            Name = Subject.Name,
            Credits = Subject.Credits,
            Description = Subject.Description,
            Price = Subject.Price,
            ProgramSemester = Subject.ProgramSemester,
            IsActive = Subject.IsActive,
            PrerequisiteSubjectIds = Subject.Prerequisites?.Select(p => p.PrerequisiteSubjectId).ToList() ?? new List<int>()
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

        // Parse prerequisite IDs from form submission
        if (!string.IsNullOrEmpty(PrerequisiteIds))
        {
            UpdateDto.PrerequisiteSubjectIds = PrerequisiteIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }
        else
        {
            UpdateDto.PrerequisiteSubjectIds = new List<int>();
        }

        if (UpdateDto.ProgramSemester is < 1 or > 9)
        {
            ModelState.AddModelError("UpdateDto.ProgramSemester", "Kỳ chương trình phải từ 1 đến 9.");
        }

        if (!ModelState.IsValid)
        {
            Subject = await _subjectService.GetByIdAsync(Id);
            ViewData["Title"] = $"Sửa: {Subject?.Name}";
            AllSubjects = await _subjectService.GetAllAsync();
            return Page();
        }

        var result = await _subjectService.UpdateAsync(Id, UpdateDto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi cập nhật.");
            Subject = await _subjectService.GetByIdAsync(Id);
            ViewData["Title"] = $"Sửa: {Subject?.Name}";
            AllSubjects = await _subjectService.GetAllAsync();
            return Page();
        }

        var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var role in roles)
        {
            await _hubContext.Clients.Group(role).SendAsync("SubjectUpdated", new
            {
                id = Id,
                action = "edit",
                name = UpdateDto.Name
            });
        }

        TempData["Success"] = $"Đã cập nhật môn học <b>{UpdateDto.Name}</b> thành công!";
        return RedirectToPage("/Admin/Subjects/Index");
    }
}
