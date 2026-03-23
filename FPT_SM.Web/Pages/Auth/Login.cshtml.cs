using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Auth;

public class LoginModel : BasePageModel
{
    private readonly IAuthService _authService;
    public LoginModel(IAuthService authService) { _authService = authService; }

    [BindProperty] public LoginDto Input { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (CurrentUserId != null) return RedirectByRole();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _authService.LoginAsync(Input);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        var user = result.Data!;
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetString("RoleName", user.RoleName);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);
        HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl ?? "");

        return RedirectByRole();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Auth/Login");
    }

    private IActionResult RedirectByRole() => CurrentRole switch
    {
        "Admin" => RedirectToPage("/Admin/Dashboard"),
        "Manager" => RedirectToPage("/Manager/Dashboard"),
        "Teacher" => RedirectToPage("/Teacher/Dashboard"),
        _ => RedirectToPage("/Student/Dashboard")
    };
}
