using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Teacher.Attendance;

public class IndexModel : BasePageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IClassService _classService;
    private readonly IHubContext<AttendanceHub> _attendanceHub;

    public IndexModel(
        IAttendanceService attendanceService,
        IClassService classService,
        IHubContext<AttendanceHub> attendanceHub)
    {
        _attendanceService = attendanceService;
        _classService = classService;
        _attendanceHub = attendanceHub;
    }

    public List<ClassDto> MyClasses { get; set; } = new();
    public AttendanceSheetDto? AttendanceSheet { get; set; }
    public List<AttendanceAlertDto> Alerts { get; set; } = new();

    [BindProperty(SupportsGet = true)] public int? SelectedClassId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedDate { get; set; }
    [BindProperty] public MarkAttendanceDto Dto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        ViewData["Title"] = "Điểm danh";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "T";
        ViewData["UserId"] = CurrentUserId;

        MyClasses = await _classService.GetByTeacherAsync(CurrentUserId!.Value);

        if (SelectedClassId.HasValue)
        {
            var date = DateTime.TryParse(SelectedDate, out var d) ? d : DateTime.Today;
            AttendanceSheet = await _attendanceService.GetAttendanceSheetAsync(SelectedClassId.Value, date);
            Alerts = await _attendanceService.GetAttendanceAlertsAsync(SelectedClassId.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAttendanceAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _attendanceService.MarkAttendanceAsync(Dto, CurrentUserId!.Value);

        if (result.IsSuccess)
        {
            await _attendanceHub.Clients.Group($"class_{Dto.ClassId}").SendAsync("AttendanceUpdated", new
            {
                classId = Dto.ClassId,
                date = Dto.Date.ToString("dd/MM/yyyy"),
                message = "Điểm danh đã được cập nhật"
            });

            var alerts = await _attendanceService.GetAttendanceAlertsAsync(Dto.ClassId);
            foreach (var student in alerts.Where(a => a.IsFailed))
            {
                await _attendanceHub.Clients.Group($"student_{student.StudentId}").SendAsync("StudentFailedByAbsence", new
                {
                    studentId = student.StudentId,
                    studentName = student.StudentName,
                    classId = Dto.ClassId,
                    absentCount = student.AbsentCount,
                    message = $"Bạn đã vắng {student.AbsentCount} buổi - rớt điểm danh"
                });
            }

            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage(new { classId = Dto.ClassId, selectedDate = Dto.Date.ToString("yyyy-MM-dd") });
    }
}
