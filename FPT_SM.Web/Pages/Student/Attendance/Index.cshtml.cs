using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Attendance;

public class IndexModel : BasePageModel
{
    private readonly IAttendanceService _attendanceService;

    public IndexModel(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    public List<StudentAttendanceSummaryDto> AttendanceSummaries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Điểm danh";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;

        AttendanceSummaries = await _attendanceService.GetStudentAttendanceAsync(CurrentUserId!.Value);

        return Page();
    }
}
