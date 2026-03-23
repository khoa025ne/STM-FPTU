using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Teacher.Grades;

public class IndexModel : BasePageModel
{
    private readonly IGradeService _gradeService;
    private readonly IClassService _classService;
    private readonly IHubContext<GradeHub> _gradeHub;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public IndexModel(
        IGradeService gradeService,
        IClassService classService,
        IHubContext<GradeHub> gradeHub,
        IHubContext<NotificationHub> notificationHub)
    {
        _gradeService = gradeService;
        _classService = classService;
        _gradeHub = gradeHub;
        _notificationHub = notificationHub;
    }

    public List<ClassDto> MyClasses { get; set; } = new();
    public GradeSheetDto? GradeSheet { get; set; }

    [BindProperty(SupportsGet = true)] public int? SelectedClassId { get; set; }
    [BindProperty] public List<UpdateGradeDto> GradeItems { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        System.Console.WriteLine($"[DEBUG-PageModel] ===== OnGetAsync STARTED =====");
        System.Console.WriteLine($"[DEBUG-PageModel] CurrentUserId={CurrentUserId}");
        System.Console.WriteLine($"[DEBUG-PageModel] SelectedClassId={SelectedClassId}");

        var auth = RequireRole("Teacher");
        if (auth != null)
        {
            System.Console.WriteLine($"[DEBUG-PageModel] ❌ Auth FAILED - Redirecting");
            return auth;
        }

        System.Console.WriteLine($"[DEBUG-PageModel] ✓ Auth passed");

        ViewData["Title"] = "Quản lý điểm";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "T";
        ViewData["UserId"] = CurrentUserId;

        MyClasses = await _classService.GetByTeacherAsync(CurrentUserId!.Value);
        System.Console.WriteLine($"[DEBUG-PageModel] MyClasses count={MyClasses.Count}");

        System.Console.WriteLine($"[DEBUG-PageModel] SelectedClassId HasValue={SelectedClassId.HasValue}");

        if (SelectedClassId.HasValue)
        {
            System.Console.WriteLine($"[DEBUG-PageModel] 🔄 Calling GetGradeSheetAsync with classId={SelectedClassId.Value}");
            GradeSheet = await _gradeService.GetGradeSheetAsync(SelectedClassId.Value);
            System.Console.WriteLine($"[DEBUG-PageModel] ✓ GradeSheet returned, Grades count={GradeSheet?.Grades.Count ?? 0}");
        }
        else
        {
            System.Console.WriteLine($"[DEBUG-PageModel] ⚠️ SelectedClassId is NULL or false");
        }

        System.Console.WriteLine($"[DEBUG-PageModel] ===== OnGetAsync COMPLETED =====");
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateGradeAsync(UpdateGradeDto dto, int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _gradeService.UpdateGradeAsync(dto, CurrentUserId!.Value);

        if (result.IsSuccess)
        {
            await _gradeHub.Clients.Group($"class_{classId}").SendAsync("GradeUpdated", new
            {
                gradeId = dto.GradeId,
                midterm = dto.MidtermScore,
                final = dto.FinalScore,
                message = "Điểm đã được cập nhật"
            });
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage(new { classId });
    }

    public async Task<IActionResult> OnPostSaveAllGradesAsync(int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        int saved = 0;
        foreach (var dto in GradeItems)
        {
            if (dto.GradeId > 0)
            {
                var result = await _gradeService.UpdateGradeAsync(dto, CurrentUserId!.Value);
                if (result.IsSuccess)
                {
                    saved++;
                    await _gradeHub.Clients.Group($"class_{classId}").SendAsync("GradeUpdated", new
                    {
                        gradeId = dto.GradeId,
                        midterm = dto.MidtermScore,
                        final = dto.FinalScore
                    });
                }
            }
        }

        TempData["Success"] = $"Đã lưu {saved} điểm thành công!";
        return RedirectToPage(new { classId });
    }

    public async Task<IActionResult> OnPostPublishGradesAsync(int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _gradeService.PublishGradesAsync(classId, CurrentUserId!.Value);

        if (result.IsSuccess)
        {
            await _gradeHub.Clients.Group($"class_{classId}").SendAsync("GradesPublished", new
            {
                classId,
                message = "Điểm đã được công bố"
            });

            // Notify each student
            var sheet = await _gradeService.GetGradeSheetAsync(classId);
            if (sheet != null)
            {
                foreach (var grade in sheet.Grades)
                {
                    await _notificationHub.Clients.Group($"student_{grade.StudentId}").SendAsync("NewNotification", new
                    {
                        title = "Điểm số đã công bố",
                        message = $"Điểm môn {sheet.SubjectName} ({sheet.ClassName}) đã được công bố. GPA: {grade.GPA:F2}",
                        type = "success"
                    });
                }
            }

            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage(new { classId });
    }

    public async Task<IActionResult> OnPostAnalyzeWithAIAsync(int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _gradeService.AnalyzeClassWithAIAsync(classId, CurrentUserId!.Value);

        if (result.IsSuccess)
        {
            await _gradeHub.Clients.Group($"class_{classId}").SendAsync("AIAnalysisComplete", new
            {
                classId,
                message = result.Message
            });
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage(new { classId });
    }
}
