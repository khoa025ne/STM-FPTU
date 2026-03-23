using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Teacher.Classes;

public class IndexModel : BasePageModel
{
    private readonly IClassService _classService;

    public IndexModel(IClassService classService)
    {
        _classService = classService;
    }

    public List<ClassDto> MyClasses { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? SemesterId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        ViewData["Title"] = "Danh sách lớp dạy";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "T";
        ViewData["UserId"] = CurrentUserId;

        MyClasses = await _classService.GetByTeacherAsync(CurrentUserId!.Value);

        if (SemesterId.HasValue)
        {
            MyClasses = MyClasses.Where(c => c.SemesterId == SemesterId.Value).ToList();
        }

        return Page();
    }
}
