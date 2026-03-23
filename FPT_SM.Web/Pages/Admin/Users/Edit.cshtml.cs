using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

namespace FPT_SM.Web.Pages.Admin.Users;

public class EditModel : BasePageModel
{
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public EditModel(IUserService userService, IHubContext<NotificationHub> hubContext)
    {
        _userService = userService;
        _hubContext = hubContext;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public UserDto? User { get; set; }

    [BindProperty]
    public EditUserInputModel Input { get; set; } = new();

    public class EditUserInputModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = null!;

        [Display(Name = "URL Avatar")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Số dư ví")]
        [Range(0, 999999999, ErrorMessage = "Số dư không hợp lệ.")]
        public decimal WalletBalance { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string? ConfirmNewPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        User = await _userService.GetByIdAsync(Id);
        if (User == null)
        {
            TempData["Error"] = "Không tìm thấy người dùng.";
            return RedirectToPage("/Admin/Users/Index");
        }

        ViewData["Title"] = $"Sửa: {User.FullName}";

        Input = new EditUserInputModel
        {
            FullName = User.FullName,
            AvatarUrl = User.AvatarUrl,
            WalletBalance = User.WalletBalance,
            IsActive = User.IsActive
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

        // Remove password validation if not filled
        if (string.IsNullOrEmpty(Input.NewPassword))
        {
            ModelState.Remove("Input.NewPassword");
            ModelState.Remove("Input.ConfirmNewPassword");
        }

        if (!ModelState.IsValid)
        {
            User = await _userService.GetByIdAsync(Id);
            ViewData["Title"] = $"Sửa: {User?.FullName}";
            return Page();
        }

        var dto = new UpdateUserDto
        {
            FullName = Input.FullName,
            AvatarUrl = Input.AvatarUrl,
            WalletBalance = Input.WalletBalance,
            IsActive = Input.IsActive
        };

        var result = await _userService.UpdateUserAsync(Id, dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi cập nhật.");
            User = await _userService.GetByIdAsync(Id);
            ViewData["Title"] = $"Sửa: {User?.FullName}";
            return Page();
        }

        // Admin password reset: use ResetPasswordAsync (sets to default or sends reset email)
        if (!string.IsNullOrEmpty(Input.NewPassword))
        {
            _ = await _userService.ResetPasswordAsync(Id);
        }

        // Broadcast user update to all roles
        var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var role in roles)
        {
            await _hubContext.Clients.Group(role).SendAsync("UserUpdated", new
            {
                id = Id,
                action = "edit",
                name = Input.FullName
            });
        }

        TempData["Success"] = $"Đã cập nhật tài khoản <b>{Input.FullName}</b> thành công!";
        return RedirectToPage("/Admin/Users/Index");
    }
}
