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

    [BindProperty(SupportsGet = true, Name = "classId")] public int? SelectedClassId { get; set; }
    [BindProperty] public List<UpdateGradeDto> GradeItems { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        ViewData["Title"] = "Quản lý điểm";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "T";
        ViewData["UserId"] = CurrentUserId;

        // Chỉ hiển thị các lớp của giáo viên trong kỳ đang diễn ra
        MyClasses = await _classService.GetByTeacherInOngoingSemesterAsync(CurrentUserId!.Value);

        if (SelectedClassId.HasValue)
        {
            GradeSheet = await _gradeService.GetGradeSheetAsync(SelectedClassId.Value);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostReopenGradesAsync(int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _gradeService.ReopenGradesAsync(classId, CurrentUserId!.Value);
        if (result.IsSuccess)
        {
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Grade",
                    action = "reopen",
                    classId
                });
            }

            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage(new { classId });
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

            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Grade",
                    action = "update",
                    classId,
                    gradeId = dto.GradeId
                });
            }

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

        if (saved > 0)
        {
            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Grade",
                    action = "bulk-update",
                    classId,
                    count = saved
                });
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

            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Grade",
                    action = "publish",
                    classId
                });
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
