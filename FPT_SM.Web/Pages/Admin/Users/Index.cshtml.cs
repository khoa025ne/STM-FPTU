using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Admin.Users;

public class IndexModel : BasePageModel
{
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public IndexModel(IUserService userService, IHubContext<NotificationHub> hubContext)
    {
        _userService = userService;
        _hubContext = hubContext;
    }

    public List<UserDto> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoleFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    private const int PageSize = 15;

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Quản lý tài khoản";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        var filter = new UserFilterDto
        {
            SearchText = SearchTerm,
            RoleId = RoleFilter,
            Page = Page,
            PageSize = PageSize
        };

        var result = await _userService.GetUsersAsync(filter);
        Users = result.Items.ToList();
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;

        return Page();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        var result = await _userService.ToggleActiveAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message ?? "Đã cập nhật trạng thái tài khoản.";
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _hubContext.Clients.Group(role).SendAsync("UserUpdated", new { id, action = "toggle" });
            }
        }
        else
        {
            TempData["Error"] = result.Message ?? "Có lỗi xảy ra.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        var result = await _userService.DeleteUserAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Đã xóa tài khoản thành công.";
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _hubContext.Clients.Group(role).SendAsync("UserUpdated", new { id, action = "delete" });
            }
        }
        else
        {
            TempData["Error"] = result.Message ?? "Không thể xóa tài khoản này.";
        }
        return RedirectToPage();
    }
}
