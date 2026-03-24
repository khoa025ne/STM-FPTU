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
    public List<ClassAttendanceSessionDto> SessionOptions { get; set; } = new();

    [BindProperty(SupportsGet = true, Name = "classId")]
    public int? SelectedClassId { get; set; }
    
    [BindProperty(SupportsGet = true, Name = "selectedDate")]
    public string? SelectedDate { get; set; }

    [BindProperty(SupportsGet = true, Name = "selectedSession")]
    public int? SelectedSession { get; set; }
    
    [BindProperty]
    public MarkAttendanceDto Dto { get; set; } = new();

    [BindProperty]
    public int EditAttendanceId { get; set; }
    
    [BindProperty]
    public string EditStatus { get; set; } = "Present";
    
    [BindProperty]
    public string? EditNote { get; set; }

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
            SessionOptions = await _attendanceService.GetClassAttendanceSessionsAsync(SelectedClassId.Value);

            DateTime date;
            if (SelectedSession.HasValue)
            {
                var selected = SessionOptions.FirstOrDefault(s => s.SessionNumber == SelectedSession.Value);
                date = selected?.Date ?? DateTime.Today;
            }
            else if (!string.IsNullOrEmpty(SelectedDate) && DateTime.TryParse(SelectedDate, out var parsedDate))
            {
                date = SessionOptions.FirstOrDefault(s => s.Date.Date == parsedDate.Date)?.Date
                    ?? parsedDate.Date;
            }
            else
            {
                date = SessionOptions.FirstOrDefault(s => s.Date.Date >= DateTime.Today)?.Date
                    ?? SessionOptions.FirstOrDefault()?.Date
                    ?? DateTime.Today;
            }

            SelectedDate = date.ToString("yyyy-MM-dd");
            AttendanceSheet = await _attendanceService.GetAttendanceSheetAsync(SelectedClassId.Value, date);
            if (AttendanceSheet != null)
            {
                SelectedSession = AttendanceSheet.SessionNumber;
            }
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

            var affectedStudentIds = Dto.Items
                .Select(i => i.StudentId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            foreach (var studentId in affectedStudentIds)
            {
                await _attendanceHub.Clients.Group($"student_{studentId}").SendAsync("AttendanceUpdated", new
                {
                    studentId,
                    classId = Dto.ClassId,
                    sessionNumber = Dto.SessionNumber,
                    date = Dto.Date.ToString("yyyy-MM-dd"),
                    message = "Điểm danh của bạn vừa được cập nhật"
                });
            }

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

        return RedirectToPage(new { classId = Dto.ClassId, selectedSession = Dto.SessionNumber });
    }

    public async Task<IActionResult> OnPostUpdateAttendanceAsync()
    {
        var auth = RequireRole("Teacher");
        if (auth != null) return auth;

        var result = await _attendanceService.UpdateAttendanceAsync(EditAttendanceId, EditStatus, EditNote);
        var attendanceInfo = await _attendanceService.GetAttendanceInfoAsync(EditAttendanceId);

        if (result.IsSuccess)
        {
            if (attendanceInfo.HasValue)
            {
                var (classId, studentId, slotDate, sessionNumber) = attendanceInfo.Value;

                await _attendanceHub.Clients.Group($"student_{studentId}").SendAsync("AttendanceUpdated", new
                {
                    studentId,
                    attendanceId = EditAttendanceId,
                    classId,
                    sessionNumber,
                    date = slotDate.ToString("yyyy-MM-dd"),
                    message = "Điểm danh của bạn đã được cập nhật"
                });
            }

            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        if (attendanceInfo.HasValue)
        {
            var (classId, _, slotDate, _) = attendanceInfo.Value;
            return RedirectToPage(new { classId = classId, selectedDate = slotDate.ToString("yyyy-MM-dd") });
        }

        // Fallback: redirect to first class
        var myClasses = await _classService.GetByTeacherAsync(CurrentUserId!.Value);
        if (myClasses.Any())
        {
            return RedirectToPage(new { classId = myClasses.First().Id, selectedDate = DateTime.Today.ToString("yyyy-MM-dd") });
        }

        return RedirectToPage();
    }
}
