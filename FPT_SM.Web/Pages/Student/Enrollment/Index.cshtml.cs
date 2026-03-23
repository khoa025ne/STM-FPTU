using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Student.Enrollment;

public class IndexModel : BasePageModel
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IWalletService _walletService;
    private readonly ISemesterService _semesterService;
    private readonly IHubContext<NotificationHub> _hub;

    public IndexModel(
        IEnrollmentService enrollmentService,
        IWalletService walletService,
        ISemesterService semesterService,
        IHubContext<NotificationHub> hub)
    {
        _enrollmentService = enrollmentService;
        _walletService = walletService;
        _semesterService = semesterService;
        _hub = hub;
    }

    public List<EnrollmentDto> CurrentEnrollments { get; set; } = new();
    public List<AvailableSubjectDto> AvailableSubjects { get; set; } = new();
    public WalletDto? Wallet { get; set; }
    public int? SelectedSemesterId { get; set; }
    public SemesterDto? CurrentSemester { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Đăng ký Học phần";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;

        var userId = CurrentUserId!.Value;

        Wallet = await _walletService.GetWalletAsync(userId);
        CurrentSemester = await _semesterService.GetOngoingAsync();

        if (CurrentSemester != null)
        {
            SelectedSemesterId = CurrentSemester.Id;
            CurrentEnrollments = await _enrollmentService.GetByStudentAsync(userId, CurrentSemester.Id);
            AvailableSubjects = await _enrollmentService.GetAvailableSubjectsAsync(userId, CurrentSemester.Id);
        }
        else
        {
            CurrentEnrollments = await _enrollmentService.GetByStudentAsync(userId);
        }

        if (TempData.ContainsKey("SuccessMessage"))
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
        if (TempData.ContainsKey("ErrorMessage"))
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];

        return Page();
    }

    public async Task<IActionResult> OnPostEnrollAsync(int classId, int semesterId)
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        var userId = CurrentUserId!.Value;

        var dto = new EnrollRegistrationDto
        {
            SemesterId = semesterId,
            ClassIds = new List<int> { classId }
        };

        var result = await _enrollmentService.ProcessEnrollmentAsync(userId, dto);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message ?? "Đăng ký học phần thành công!";

            var wallet = await _walletService.GetWalletAsync(userId);
            await _hub.Clients.Group($"student_{userId}")
                .SendAsync("WalletUpdated", wallet.Balance);

            // Fetch class info for cross-role broadcasting
            var classService = HttpContext.RequestServices.GetRequiredService<IClassService>();
            var classInfo = await classService.GetByIdAsync(classId);

            if (classInfo != null)
            {
                // Notify teacher (roster change)
                await _hub.Clients.Group($"teacher_{classInfo.TeacherId}")
                    .SendAsync("EnrollmentChanged", new
                    {
                        action = "enroll",
                        studentName = CurrentFullName,
                        className = classInfo.Name,
                        enrolled = classInfo.Enrolled,
                        capacity = classInfo.Capacity,
                        message = $"{CurrentFullName} đã đăng ký {classInfo.Name}"
                    });

                // Notify managers (capacity monitoring)
                await _hub.Clients.Group("manager_all")
                    .SendAsync("EnrollmentChanged", new
                    {
                        action = "enroll",
                        className = classInfo.Name,
                        enrolled = classInfo.Enrolled,
                        capacity = classInfo.Capacity
                    });

                // Notify all students (availability update)
                await _hub.Clients.Group("student_all")
                    .SendAsync("ClassCapacityChanged", new
                    {
                        classId,
                        className = classInfo.Name,
                        enrolled = classInfo.Enrolled,
                        capacity = classInfo.Capacity,
                        remainingSlots = classInfo.Capacity - classInfo.Enrolled
                    });
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Đăng ký thất bại. Vui lòng thử lại.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDropAsync(int enrollmentId)
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        var userId = CurrentUserId!.Value;
        var result = await _enrollmentService.DropEnrollmentAsync(enrollmentId, userId);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message ?? "Đã rút môn học thành công.";
            await _hub.Clients.Group($"student_{userId}")
                .SendAsync("EnrollmentUpdated", "drop");

            // Fetch enrollment details for cross-role broadcasting
            var enrollments = await _enrollmentService.GetByStudentAsync(userId);
            var droppedEnrollment = enrollments.FirstOrDefault(e => e.Id == enrollmentId);

            if (droppedEnrollment != null)
            {
                var classService = HttpContext.RequestServices.GetRequiredService<IClassService>();
                var classInfo = await classService.GetByIdAsync(droppedEnrollment.ClassId);

                if (classInfo != null)
                {
                    // Notify teacher (roster change)
                    await _hub.Clients.Group($"teacher_{classInfo.TeacherId}")
                        .SendAsync("EnrollmentChanged", new
                        {
                            action = "drop",
                            studentName = CurrentFullName,
                            className = classInfo.Name,
                            enrolled = classInfo.Enrolled,
                            capacity = classInfo.Capacity,
                            message = $"{CurrentFullName} đã rút khỏi {classInfo.Name}"
                        });

                    // Notify managers (capacity monitoring)
                    await _hub.Clients.Group("manager_all")
                        .SendAsync("EnrollmentChanged", new
                        {
                            action = "drop",
                            className = classInfo.Name,
                            enrolled = classInfo.Enrolled,
                            capacity = classInfo.Capacity
                        });

                    // Notify all students (slot opened up)
                    await _hub.Clients.Group("student_all")
                        .SendAsync("ClassCapacityChanged", new
                        {
                            classId = droppedEnrollment.ClassId,
                            className = classInfo.Name,
                            enrolled = classInfo.Enrolled,
                            capacity = classInfo.Capacity,
                            remainingSlots = classInfo.Capacity - classInfo.Enrolled
                        });
                }
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Rút môn thất bại. Vui lòng thử lại.";
        }

        return RedirectToPage();
    }
}
