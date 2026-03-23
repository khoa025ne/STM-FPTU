using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Quizzes;

public class IndexModel : BasePageModel
{
    private readonly IQuizService _quizService;

    public IndexModel(IQuizService quizService)
    {
        _quizService = quizService;
    }

    public List<QuizDto> AvailableQuizzes { get; set; } = new();
    public List<QuizDto> CompletedQuizzes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Quiz của tôi";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;

        var all = await _quizService.GetForStudentAsync(CurrentUserId!.Value);
        AvailableQuizzes = all.Where(q => !q.HasSubmitted && !q.IsExpired).ToList();
        CompletedQuizzes = all.Where(q => q.HasSubmitted || q.IsExpired).ToList();

        return Page();
    }
}
