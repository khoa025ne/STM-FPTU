using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Quizzes;

public class TakeModel : BasePageModel
{
    private readonly IQuizService _quizService;

    public TakeModel(IQuizService quizService)
    {
        _quizService = quizService;
    }

    public QuizDetailDto? Quiz { get; set; }
    public int SubmissionId { get; set; }
    public QuizResultDto? Result { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Làm bài Quiz";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;

        Quiz = await _quizService.GetDetailAsync(id, CurrentUserId!.Value);

        if (Quiz == null)
        {
            TempData["Error"] = "Không tìm thấy quiz.";
            return RedirectToPage("/Student/Quizzes/Index");
        }

        if (Quiz.HasSubmitted)
        {
            TempData["Info"] = "Bạn đã nộp bài quiz này rồi.";
            return RedirectToPage("/Student/Quizzes/Index");
        }

        // Start attempt
        var attemptResult = await _quizService.StartAttemptAsync(id, CurrentUserId!.Value);
        if (!attemptResult.IsSuccess)
        {
            TempData["Error"] = attemptResult.Message;
            return RedirectToPage("/Student/Quizzes/Index");
        }

        SubmissionId = attemptResult.Data;
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(SubmitQuizDto dto)
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        var result = await _quizService.SubmitAsync(dto, CurrentUserId!.Value);

        if (result.IsSuccess)
        {
            TempData["Success"] = $"Nộp bài thành công! Điểm của bạn: {result.Data!.TotalScore:F1}/{result.Data.MaxScore:F1}";
            return RedirectToPage("/Student/Quizzes/Index");
        }

        TempData["Error"] = result.Message;
        return RedirectToPage("/Student/Quizzes/Index");
    }
}
