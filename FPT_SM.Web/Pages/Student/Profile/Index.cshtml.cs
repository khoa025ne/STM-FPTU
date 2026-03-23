using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

namespace FPT_SM.Web.Pages.Student.Profile;

public class IndexModel : BasePageModel
{
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hub;

    public IndexModel(IUserService userService, IHubContext<NotificationHub> hub)
    {
        _userService = userService;
        _hub = hub;
    }

    public UserDto? User { get; set; }

    [BindProperty]
    public UpdateProfileInputModel ProfileInput { get; set; } = new();

    [BindProperty]
    public ChangePasswordInputModel PasswordInput { get; set; } = new();

    public class UpdateProfileInputModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = null!;

        [Display(Name = "URL Ảnh đại diện")]
        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordInputModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string OldPassword { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = null!;
    }

    private async Task LoadUserAsync()
    {
        if (CurrentUserId.HasValue)
            User = await _userService.GetByIdAsync(CurrentUserId.Value);

        ViewData["Title"] = "Hồ sơ của tôi";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["AvatarUrl"] = CurrentAvatarUrl;
        ViewData["UserId"] = CurrentUserId;
        ViewData["RollNumber"] = User?.RollNumber;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = RequireRole("Student", "Teacher", "Admin", "Manager");
        if (redirect != null) return redirect;

        await LoadUserAsync();

        if (User == null) return RedirectToPage("/Auth/Login");

        ProfileInput = new UpdateProfileInputModel
        {
            FullName = User.FullName,
            AvatarUrl = User.AvatarUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var redirect = RequireRole("Student", "Teacher", "Admin", "Manager");
        if (redirect != null) return redirect;

        // Remove password validation keys
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("PasswordInput")).ToList())
            ModelState.Remove(key);

        await LoadUserAsync();

        if (!ModelState.IsValid)
            return Page();

        var dto = new UpdateUserDto
        {
            FullName = ProfileInput.FullName,
            AvatarUrl = ProfileInput.AvatarUrl,
            WalletBalance = User?.WalletBalance ?? 0,
            IsActive = User?.IsActive ?? true
        };

        var result = await _userService.UpdateUserAsync(CurrentUserId!.Value, dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra khi cập nhật.");
            return Page();
        }

        // Update session
        HttpContext.Session.SetString("FullName", ProfileInput.FullName);
        if (!string.IsNullOrEmpty(ProfileInput.AvatarUrl))
            HttpContext.Session.SetString("AvatarUrl", ProfileInput.AvatarUrl);

        TempData["Success"] = "Đã cập nhật thông tin cá nhân thành công!";
        var updatedAvatar = string.IsNullOrWhiteSpace(ProfileInput.AvatarUrl)
            ? User?.AvatarUrl
            : ProfileInput.AvatarUrl;
        await _hub.Clients.Group($"student_{CurrentUserId!.Value}")
            .SendAsync("AvatarUpdated", new
            {
                avatarUrl = updatedAvatar,
                fullName = ProfileInput.FullName,
                rollNumber = User?.RollNumber
            });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var redirect = RequireRole("Student", "Teacher", "Admin", "Manager");
        if (redirect != null) return redirect;

        // Remove profile validation keys
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("ProfileInput")).ToList())
            ModelState.Remove(key);

        await LoadUserAsync();

        if (!ModelState.IsValid)
            return Page();

        var dto = new ChangePasswordDto
        {
            OldPassword = PasswordInput.OldPassword,
            NewPassword = PasswordInput.NewPassword,
            ConfirmPassword = PasswordInput.ConfirmPassword
        };

        var result = await _userService.ChangePasswordAsync(CurrentUserId!.Value, dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("PasswordInput.OldPassword", result.Message ?? "Mật khẩu hiện tại không đúng.");
            return Page();
        }

        TempData["Success"] = "Đã đổi mật khẩu thành công!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadAvatarAsync(IFormFile avatarFile)
    {
        var redirect = RequireRole("Student", "Teacher", "Admin", "Manager");
        if (redirect != null) return redirect;

        await LoadUserAsync();

        if (avatarFile == null || avatarFile.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file ảnh.";
            return RedirectToPage();
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(avatarFile.ContentType.ToLower()))
        {
            TempData["Error"] = "Chỉ chấp nhận file ảnh JPG, PNG, GIF, WEBP.";
            return RedirectToPage();
        }

        if (avatarFile.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "File ảnh không được vượt quá 5MB.";
            return RedirectToPage();
        }

        using var stream = avatarFile.OpenReadStream();
        var result = await _userService.UpdateAvatarAsync(
            CurrentUserId!.Value,
            avatarFile.FileName,
            stream,
            avatarFile.ContentType);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Message ?? "Không thể tải lên ảnh.";
            return RedirectToPage();
        }

        if (!string.IsNullOrEmpty(result.Data))
            HttpContext.Session.SetString("AvatarUrl", result.Data);

        TempData["Success"] = "Đã cập nhật ảnh đại diện thành công!";
        var broadcastAvatar = result.Data ?? User?.AvatarUrl;
        await _hub.Clients.Group($"student_{CurrentUserId!.Value}")
            .SendAsync("AvatarUpdated", new
            {
                avatarUrl = broadcastAvatar,
                fullName = User?.FullName,
                rollNumber = User?.RollNumber
            });
        return RedirectToPage();
    }
}
