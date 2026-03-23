using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FPT_SM.Web.Pages;

public abstract class BasePageModel : PageModel
{
    public int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
    public string? CurrentRole => HttpContext.Session.GetString("RoleName");
    public string? CurrentFullName => HttpContext.Session.GetString("FullName");
    public string? CurrentEmail => HttpContext.Session.GetString("Email");
    public string? CurrentAvatarUrl => HttpContext.Session.GetString("AvatarUrl");

    protected IActionResult? RequireRole(params string[] roles)
    {
        if (CurrentUserId == null) return RedirectToPage("/Auth/Login");
        if (!roles.Contains(CurrentRole)) return RedirectToPage("/Auth/Login");
        return null;
    }
}
