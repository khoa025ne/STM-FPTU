using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

namespace FPT_SM.Web.Pages.Admin.Users;

public class CreateModel : BasePageModel
{
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreateModel(IUserService userService, IHubContext<NotificationHub> hubContext)
    {
        _userService = userService;
        _hubContext = hubContext;
    }

    [BindProperty]
    public CreateUserInputModel Input { get; set; } = new();

    public class CreateUserInputModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        [Display(Name = "Vai trò")]
        [Range(1, 4, ErrorMessage = "Vui lòng chọn vai trò hợp lệ.")]
        public int RoleId { get; set; }

        [Display(Name = "Số dư ví ban đầu")]
        [Range(0, 999999999, ErrorMessage = "Số dư không hợp lệ.")]
        public decimal WalletBalance { get; set; } = 0;
    }

    public IActionResult OnGet()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Thêm người dùng mới";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var redirect = RequireRole("Admin");
        if (redirect != null) return redirect;

        ViewData["Title"] = "Thêm người dùng mới";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "A";
        ViewData["UserId"] = CurrentUserId;

        if (!ModelState.IsValid)
            return Page();

        var dto = new CreateUserDto
        {
            FullName = Input.FullName,
            Email = Input.Email,
            Password = Input.Password,
            RoleId = Input.RoleId,
            WalletBalance = Input.WalletBalance
        };

        var result = await _userService.CreateUserAsync(dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi tạo tài khoản.");
            return Page();
        }

        // Broadcast user creation to all roles
        var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
        foreach (var role in roles)
        {
            await _hubContext.Clients.Group(role).SendAsync("UserUpdated", new
            {
                id = result.Data?.Id ?? 0,
                action = "create",
                name = result.Data?.FullName
            });
        }

        TempData["Success"] = $"Đã tạo tài khoản <b>{Input.FullName}</b> thành công!";
        return RedirectToPage("/Admin/Users/Index");
    }
}
