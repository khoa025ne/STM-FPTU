using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceSheetDto?> GetAttendanceSheetAsync(int classId, DateTime date);
    Task<ServiceResult> MarkAttendanceAsync(MarkAttendanceDto dto, int teacherId);
    Task<ServiceResult> UpdateAttendanceAsync(int attendanceId, string status, string? note);
    Task<List<StudentAttendanceSummaryDto>> GetStudentAttendanceAsync(int studentId, int? semesterId = null);
    Task<StudentAttendanceSummaryDto?> GetEnrollmentAttendanceAsync(int enrollmentId);
    Task<List<AttendanceAlertDto>> GetAttendanceAlertsAsync(int classId);
    Task<List<AttendanceHistoryDto>> GetAttendanceHistoryAsync(int classId);
    Task<(int ClassId, DateTime SlotDate)?> GetAttendanceInfoAsync(int attendanceId);
}
