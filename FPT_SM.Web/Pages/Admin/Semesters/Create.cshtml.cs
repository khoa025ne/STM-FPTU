using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Semesters;

public class CreateModel : BasePageModel
{
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreateModel(ISemesterService semesterService, IHubContext<NotificationHub> hubContext)
    {
        _semesterService = semesterService;
        _hubContext = hubContext;
    }

    [BindProperty]
    public CreateSemesterDto CreateDto { get; set; } = new();

    public IActionResult OnGet()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Tạo học kỳ";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Tạo học kỳ";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        if (!ModelState.IsValid)
            return Page();

        var result = await _semesterService.CreateAsync(CreateDto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi tạo học kỳ.");
            return Page();
        }

        var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var role in roles)
        {
            await _hubContext.Clients.Group(role).SendAsync("SemesterUpdated", new
            {
                id = result.Data?.Id ?? 0,
                action = "create",
                name = result.Data?.Name
            });
        }

        TempData["Success"] = $"Đã tạo học kỳ <b>{CreateDto.Name}</b> thành công!";
        return RedirectToPage("/Admin/Semesters/Index");
    }
}
