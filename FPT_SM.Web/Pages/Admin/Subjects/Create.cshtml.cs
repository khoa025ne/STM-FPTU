using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Subjects;

public class CreateModel : BasePageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreateModel(ISubjectService subjectService, IHubContext<NotificationHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    public List<SubjectDto> AllSubjects { get; set; } = new();

    [BindProperty]
    public CreateSubjectDto CreateDto { get; set; } = new();

    [BindProperty]
    public string? PrerequisiteIds { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Tạo môn học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        AllSubjects = await _subjectService.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Tạo môn học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        AllSubjects = await _subjectService.GetAllAsync();

        // Parse prerequisite IDs from form submission
        if (!string.IsNullOrEmpty(PrerequisiteIds))
        {
            CreateDto.PrerequisiteSubjectIds = PrerequisiteIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }

        if (!ModelState.IsValid)
            return Page();

        var result = await _subjectService.CreateAsync(CreateDto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi tạo môn học.");
            return Page();
        }

        var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var role in roles)
        {
            await _hubContext.Clients.Group(role).SendAsync("SubjectUpdated", new
            {
                id = result.Data?.Id ?? 0,
                action = "create",
                name = result.Data?.Name
            });
        }

        TempData["Success"] = $"Đã tạo môn học <b>{CreateDto.Name}</b> thành công!";
        return RedirectToPage("/Admin/Subjects/Index");
    }
}
