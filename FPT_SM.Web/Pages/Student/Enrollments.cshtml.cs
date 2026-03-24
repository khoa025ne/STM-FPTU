using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student;

public class EnrollmentsModel : BasePageModel
{
    public IActionResult OnGet()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        return RedirectToPage("/Student/Enrollment/Index");
    }
}
