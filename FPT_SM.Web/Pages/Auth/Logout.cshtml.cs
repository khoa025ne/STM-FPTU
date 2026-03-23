using FPT_SM.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Auth;

public class LogoutModel : BasePageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Auth/Login");
    }
}
