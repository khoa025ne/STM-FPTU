using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Teacher.Quizzes;

public class IndexModel : BasePageModel
{
    private readonly IQuizService _quizService;
    private readonly IClassService _classService;
    private readonly IHubContext<QuizHub> _quizHub;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public IndexModel(IQuizService quizService, IClassService classService, IHubContext<QuizHub> quizHub, IHubContext<NotificationHub> notificationHub)
    {
        _quizService = quizService;
        _classService = classService;
        _quizHub = quizHub;
        _notificationHub = notificationHub;
    }

    public List<ClassDto> MyClasses { get; set; } = new();
    public List<QuizDto> Quizzes { get; set; } = new();

    [BindProperty(SupportsGet = true)] public int? SelectedClassId { get; set; }
    [BindProperty] public CreateQuizDto CreateDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        ViewData["Title"] = "Quản lý Quiz";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "T";
        ViewData["UserId"] = CurrentUserId;

        MyClasses = await _classService.GetByTeacherAsync(CurrentUserId!.Value);

        SelectedClassId = classId ?? SelectedClassId;
        if (SelectedClassId.HasValue)
            Quizzes = await _quizService.GetByClassAsync(SelectedClassId.Value);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _quizService.CreateAsync(CreateDto);

        if (result.IsSuccess)
        {
            // Broadcast quiz creation to all students in class
            await _quizHub.Clients.Group($"class_{CreateDto.ClassId}").SendAsync("QuizCreated", new
            {
                quizId = result.Data?.Id ?? 0,
                title = result.Data?.Title ?? CreateDto.Title,
                classId = CreateDto.ClassId,
                startTime = CreateDto.StartTime,
                endTime = CreateDto.EndTime,
                message = "Giáo viên vừa tạo quiz mới!"
            });

            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Quiz",
                    action = "create",
                    classId = CreateDto.ClassId,
                    quizId = result.Data?.Id ?? 0
                });
            }

            TempData["Success"] = result.Message;
        }
        else
            TempData["Error"] = result.Message;

        return RedirectToPage(new { classId = CreateDto.ClassId });
    }

    public async Task<IActionResult> OnPostGenerateWithAiAsync([FromBody] GenerateQuizFromTextDto dto)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        if (dto == null)
            return new JsonResult(new { isSuccess = false, message = "Dữ liệu không hợp lệ." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

        var myClasses = await _classService.GetByTeacherAsync(CurrentUserId!.Value);
        if (!myClasses.Any(x => x.Id == dto.ClassId))
            return new JsonResult(new { isSuccess = false, message = "Bạn không có quyền tạo quiz cho lớp này." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

        var result = await _quizService.GenerateDraftFromTextAsync(dto);
        if (!result.IsSuccess)
            return new JsonResult(new { isSuccess = false, message = result.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

        return new JsonResult(new
        {
            isSuccess = true,
            message = result.Message,
            data = result.Data
        });
    }

    public async Task<IActionResult> OnPostPublishAsync(int quizId, int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _quizService.PublishAsync(quizId);

        if (result.IsSuccess)
        {
            await _quizHub.Clients.Group($"class_{classId}").SendAsync("QuizPublished", new
            {
                quizId,
                title = result.Data?.Title,
                message = "Quiz mới đã được công bố!"
            });

            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Quiz",
                    action = "publish",
                    classId,
                    quizId
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

    public async Task<IActionResult> OnPostDeleteAsync(int quizId, int classId)
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _quizService.DeleteAsync(quizId);

        if (result.IsSuccess)
        {
            // Broadcast quiz deletion to all students in class
            await _quizHub.Clients.Group($"class_{classId}").SendAsync("QuizDeleted", new
            {
                quizId,
                classId,
                message = "Quiz đã bị xóa"
            });

            var roles = new[] { "admin_all", "manager_all", "teacher_all", "student_all" };
            foreach (var role in roles)
            {
                await _notificationHub.Clients.Group(role).SendAsync("EntityChanged", new
                {
                    entity = "Quiz",
                    action = "delete",
                    classId,
                    quizId
                });
            }

            TempData["Success"] = result.Message;
        }
        else
            TempData["Error"] = result.Message;

        return RedirectToPage(new { classId });
    }
}
