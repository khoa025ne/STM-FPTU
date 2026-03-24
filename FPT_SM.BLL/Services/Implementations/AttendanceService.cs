using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.BLL.Services.Implementations;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly AppDbContext _context;

    public AttendanceService(IAttendanceRepository attendanceRepo, IEnrollmentRepository enrollmentRepo, AppDbContext context)
    {
        _attendanceRepo = attendanceRepo;
        _enrollmentRepo = enrollmentRepo;
        _context = context;
    }

    public async Task<List<ClassAttendanceSessionDto>> GetClassAttendanceSessionsAsync(int classId)
    {
        var cls = await _context.Classes
            .Include(c => c.Semester)
            .Include(c => c.Slot)
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (cls?.Semester == null || cls.Slot == null)
            return new List<ClassAttendanceSessionDto>();

        return BuildClassSessions(cls.Semester.StartDate.Date, cls.Semester.EndDate.Date, cls.Slot.DayOfWeek)
            .Select(x => new ClassAttendanceSessionDto
            {
                SessionNumber = x.SessionNumber,
                Date = x.Date
            })
            .ToList();
    }

    public async Task<AttendanceSheetDto?> GetAttendanceSheetAsync(int classId, DateTime date)
    {
        var enrollments = await _enrollmentRepo.GetByClassAsync(classId);
        var activeEnrollments = enrollments.Where(e => e.Status is 2 or 3).ToList();
        var cls = await _context.Classes
            .Include(c => c.Subject)
            .Include(c => c.Semester)
            .Include(c => c.Slot)
            .FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null) return null;
        if (cls.Semester == null || cls.Slot == null) return null;

        var sessionOptions = BuildClassSessions(
            cls.Semester.StartDate.Date,
            cls.Semester.EndDate.Date,
            cls.Slot.DayOfWeek);

        if (!sessionOptions.Any())
        {
            return new AttendanceSheetDto
            {
                ClassId = classId,
                ClassName = cls.Name,
                SubjectName = cls.Subject?.Name ?? "",
                Date = date.Date,
                SessionNumber = 1,
                TotalSessions = 0,
                Attendances = new List<AttendanceDto>()
            };
        }

        // Match exactly by schedule date; if not found, pick closest future then fallback to last.
        var selectedSession = sessionOptions.FirstOrDefault(s => s.Date.Date == date.Date);
        if (selectedSession == default)
        {
            selectedSession = sessionOptions.FirstOrDefault(s => s.Date.Date >= DateTime.Today);
        }
        if (selectedSession == default)
        {
            selectedSession = sessionOptions.Last();
        }

        var sessionNumber = selectedSession.SessionNumber;
        var sessionDate = selectedSession.Date;
        var totalSessions = sessionOptions.Count;
        var isFutureSession = sessionDate.Date > DateTime.Today;

        var items = new List<AttendanceDto>();

        foreach (var e in activeEnrollments)
        {
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => 
                a.EnrollmentId == e.Id && a.SlotDate.Date == sessionDate.Date && a.SessionNumber == sessionNumber);
            
            var absentCount = await _attendanceRepo.CountAbsentAsync(e.Id);
            items.Add(new AttendanceDto
            {
                Id = attendance?.Id ?? 0,
                EnrollmentId = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student?.FullName ?? "",
                StudentRollNumber = e.Student?.RollNumber,
                SlotDate = sessionDate,
                SessionNumber = sessionNumber,
                Status = attendance?.Status ?? (isFutureSession ? "Upcoming" : "Present"),
                Note = attendance?.Note,
                IsEditable = !isFutureSession && (attendance == null || (DateTime.Now - attendance.SlotDate).TotalDays <= 14)
            });
        }

        return new AttendanceSheetDto
        {
            ClassId = classId,
            ClassName = cls.Name,
            SubjectName = cls.Subject?.Name ?? "",
            Date = sessionDate,
            SessionNumber = sessionNumber,
            TotalSessions = totalSessions,
            Attendances = items
        };
    }

    public async Task<ServiceResult> MarkAttendanceAsync(MarkAttendanceDto dto, int teacherId)
    {
        if (dto?.Items == null || dto.Items.Count == 0)
            return ServiceResult.Failure("Không có dữ liệu điểm danh để lưu!");

        var errorCount = 0;
        var successCount = 0;
        var errors = new List<string>();
        var enrollmentsToCheckFail = new HashSet<int>(); // Track enrollments that got marked absent

        foreach (var item in dto.Items)
        {
            try
            {
                var enrollment = await _enrollmentRepo.GetByIdAsync(item.EnrollmentId);
                if (enrollment == null)
                {
                    errorCount++;
                    continue;
                }
                
                // Skip if already failed
                if (enrollment.Status == 5)
                {
                    errorCount++;
                    continue;
                }

                // Check for existing attendance by Date AND SessionNumber (allow multiple sessions same date)
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EnrollmentId == item.EnrollmentId 
                        && a.SlotDate.Date == dto.Date.Date
                        && a.SessionNumber == dto.SessionNumber);

                if (existing != null)
                {
                    // Update existing record - check if it's still editable (within 14 days)
                    if (!existing.IsEditable)
                    {
                        errorCount++;
                        var daysOld = (DateTime.Now - existing.SlotDate).Days;
                        errors.Add($"Không thể sửa: Buổi quá cũ ({daysOld} ngày - tối đa 14 ngày)");
                        continue;
                    }
                    
                    // Only track if changing TO absent
                    if (item.Status == "Absent" && existing.Status != "Absent")
                    {
                        enrollmentsToCheckFail.Add(item.EnrollmentId);
                    }
                    
                    existing.Status = item.Status;
                    existing.Note = item.Note;
                    _context.Attendances.Update(existing);
                    successCount++;
                }
                else
                {
                    // Create new record
                    if (item.Status == "Absent")
                    {
                        enrollmentsToCheckFail.Add(item.EnrollmentId);
                    }
                    
                    _context.Attendances.Add(new Attendance
                    {
                        EnrollmentId = item.EnrollmentId,
                        SlotDate = dto.Date,
                        SessionNumber = dto.SessionNumber,
                        Status = item.Status,
                        Note = item.Note
                    });
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"Lỗi: {ex.Message}");
                continue;
            }
        }

        // Save all changes first
        await _context.SaveChangesAsync();

        // Now check and update enrollment status for those with absent >= 5
        foreach (var enrollmentId in enrollmentsToCheckFail)
        {
            var absentCount = await _attendanceRepo.CountAbsentAsync(enrollmentId);
            if (absentCount >= 5)
            {
                var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId);
                if (enrollment != null && enrollment.Status != 5)
                {
                    enrollment.Status = 5; // Mark as failed due to absence
                    _context.Enrollments.Update(enrollment);
                }
            }
        }

        await _context.SaveChangesAsync();

        // Build detailed error message
        string message = "";
        if (errorCount > 0 && successCount == 0)
        {
            message = "Lưu thất bại! ";
            if (errors.Count > 0) message += errors.First();
            return ServiceResult.Failure(message);
        }
        
        if (errorCount > 0)
        {
            message = $"⚠ Lưu {successCount} sinh viên, {errorCount} sinh viên thất bại";
            if (errors.Count > 0) message += $" - {errors.First()}";
            return ServiceResult.Success(message);
        }
        
        return ServiceResult.Success($"✓ Đã lưu điểm danh cho {successCount} sinh viên thành công!");
    }

    public async Task<ServiceResult> UpdateAttendanceAsync(int attendanceId, string status, string? note)
    {
        var attendance = await _attendanceRepo.GetByIdAsync(attendanceId);
        if (attendance == null) 
            return ServiceResult.Failure("Không tìm thấy bản ghi điểm danh.");
        
        // Check if it's still editable (within 14 days)
        var daysOld = (DateTime.Now - attendance.SlotDate).Days;
        if (daysOld > 14) 
            return ServiceResult.Failure($"Không thể chỉnh sửa: Buổi quá cũ ({daysOld} ngày - tối đa 14 ngày).");

        var oldStatus = attendance.Status;
        attendance.Status = status;
        attendance.Note = note;
        await _attendanceRepo.UpdateAsync(attendance);

        // After updating, check if we need to update enrollment status
        var enrollment = await _enrollmentRepo.GetByIdAsync(attendance.EnrollmentId);
        if (enrollment == null)
            return ServiceResult.Success("Cập nhật điểm danh thành công!");

        // If changing TO "Absent", check if total absent >= 5
        if (status == "Absent" && oldStatus != "Absent")
        {
            var absentCount = await _attendanceRepo.CountAbsentAsync(attendance.EnrollmentId);
            if (absentCount >= 5 && enrollment.Status != 5)
            {
                enrollment.Status = 5; // Mark as failed due to absence
                await _enrollmentRepo.UpdateAsync(enrollment);
            }
        }
        // If changing FROM "Absent" to something else, check if we should remove failed status
        else if (oldStatus == "Absent" && status != "Absent")
        {
            var absentCount = await _attendanceRepo.CountAbsentAsync(attendance.EnrollmentId);
            // Only remove failed status if absent count is now < 5
            if (absentCount < 5 && enrollment.Status == 5)
            {
                enrollment.Status = 3; // Change back to active (assuming 3 = active)
                await _enrollmentRepo.UpdateAsync(enrollment);
            }
        }

        return ServiceResult.Success("Cập nhật điểm danh thành công!");
    }

    public async Task<List<StudentAttendanceSummaryDto>> GetStudentAttendanceAsync(int studentId, int? semesterId = null)
    {
        var enrollments = await _enrollmentRepo.GetByStudentAsync(studentId, semesterId);
        var result = new List<StudentAttendanceSummaryDto>();
        var today = DateTime.Today;

        foreach (var e in enrollments.Where(e => e.Status != 4))
        {
            var attendances = await _attendanceRepo.GetByEnrollmentAsync(e.Id);
            var scheduleSessions = (e.Semester != null && e.Class?.Slot != null)
                ? BuildClassSessions(e.Semester.StartDate.Date, e.Semester.EndDate.Date, e.Class.Slot.DayOfWeek)
                : new List<(int SessionNumber, DateTime Date)>();

            var attendanceByScheduleKey = attendances
                .GroupBy(a => new { Date = a.SlotDate.Date, a.SessionNumber })
                .ToDictionary(g => (g.Key.Date, g.Key.SessionNumber), g => g.OrderByDescending(x => x.Id).First());

            var dtoList = scheduleSessions.Any()
                ? scheduleSessions
                    .Select(s =>
                    {
                        attendanceByScheduleKey.TryGetValue((s.Date.Date, s.SessionNumber), out var attendance);
                        var isFutureSession = s.Date.Date > today;

                        return new AttendanceDto
                        {
                            Id = attendance?.Id ?? 0,
                            EnrollmentId = e.Id,
                            SlotDate = s.Date,
                            SessionNumber = s.SessionNumber,
                            Status = attendance?.Status ?? (isFutureSession ? "Upcoming" : "Present"),
                            Note = attendance?.Note,
                            IsEditable = attendance?.IsEditable ?? false
                        };
                    })
                    .ToList()
                : attendances
                    .Select(a => new AttendanceDto
                    {
                        Id = a.Id,
                        EnrollmentId = a.EnrollmentId,
                        SlotDate = a.SlotDate,
                        SessionNumber = a.SessionNumber,
                        Status = a.SlotDate.Date > today ? "Upcoming" : a.Status,
                        Note = a.Note,
                        IsEditable = a.IsEditable
                    })
                    .ToList();

            var completedSessions = dtoList.Where(a => a.SlotDate.Date <= today).ToList();

            result.Add(new StudentAttendanceSummaryDto
            {
                EnrollmentId = e.Id,
                SubjectName = e.Class?.Subject?.Name ?? "",
                SubjectCode = e.Class?.Subject?.Code ?? "",
                ClassName = e.Class?.Name ?? "",
                TotalSessions = completedSessions.Count,
                PresentCount = completedSessions.Count(a => a.Status == "Present"),
                AbsentCount = completedSessions.Count(a => a.Status == "Absent"),
                LateCount = completedSessions.Count(a => a.Status == "Late"),
                ExcusedCount = completedSessions.Count(a => a.Status == "Excused"),
                Sessions = dtoList
            });
        }
        return result;
    }

    public async Task<StudentAttendanceSummaryDto?> GetEnrollmentAttendanceAsync(int enrollmentId)
    {
        var enrollment = await _enrollmentRepo.GetWithDetailsAsync(enrollmentId);
        if (enrollment == null) return null;
        var attendances = await _attendanceRepo.GetByEnrollmentAsync(enrollmentId);
        var today = DateTime.Today;
        var scheduleSessions = (enrollment.Semester != null && enrollment.Class?.Slot != null)
            ? BuildClassSessions(enrollment.Semester.StartDate.Date, enrollment.Semester.EndDate.Date, enrollment.Class.Slot.DayOfWeek)
            : new List<(int SessionNumber, DateTime Date)>();

        var attendanceByScheduleKey = attendances
            .GroupBy(a => new { Date = a.SlotDate.Date, a.SessionNumber })
            .ToDictionary(g => (g.Key.Date, g.Key.SessionNumber), g => g.OrderByDescending(x => x.Id).First());

        var dtoList = scheduleSessions.Any()
            ? scheduleSessions
                .Select(s =>
                {
                    attendanceByScheduleKey.TryGetValue((s.Date.Date, s.SessionNumber), out var attendance);
                    var isFutureSession = s.Date.Date > today;

                    return new AttendanceDto
                    {
                        Id = attendance?.Id ?? 0,
                        EnrollmentId = enrollmentId,
                        SlotDate = s.Date,
                        SessionNumber = s.SessionNumber,
                        Status = attendance?.Status ?? (isFutureSession ? "Upcoming" : "Present"),
                        Note = attendance?.Note,
                        IsEditable = attendance?.IsEditable ?? false
                    };
                })
                .ToList()
            : attendances
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    EnrollmentId = a.EnrollmentId,
                    SlotDate = a.SlotDate,
                    SessionNumber = a.SessionNumber,
                    Status = a.SlotDate.Date > today ? "Upcoming" : a.Status,
                    Note = a.Note,
                    IsEditable = a.IsEditable
                })
                .ToList();

        var completedSessions = dtoList.Where(a => a.SlotDate.Date <= today).ToList();

        return new StudentAttendanceSummaryDto
        {
            EnrollmentId = enrollmentId,
            SubjectName = enrollment.Class?.Subject?.Name ?? "",
            SubjectCode = enrollment.Class?.Subject?.Code ?? "",
            ClassName = enrollment.Class?.Name ?? "",
            TotalSessions = completedSessions.Count,
            PresentCount = completedSessions.Count(a => a.Status == "Present"),
            AbsentCount = completedSessions.Count(a => a.Status == "Absent"),
            LateCount = completedSessions.Count(a => a.Status == "Late"),
            ExcusedCount = completedSessions.Count(a => a.Status == "Excused"),
            Sessions = dtoList
        };
    }

    public async Task<List<AttendanceAlertDto>> GetAttendanceAlertsAsync(int classId)
    {
        var enrollments = await _enrollmentRepo.GetByClassAsync(classId);
        var alerts = new List<AttendanceAlertDto>();
        foreach (var e in enrollments.Where(en => en.Status is 2 or 5))
        {
            var absent = await _attendanceRepo.CountAbsentAsync(e.Id);
            if (absent >= 3)
            {
                alerts.Add(new AttendanceAlertDto
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "",
                    RollNumber = e.Student?.RollNumber,
                    ClassName = e.Class?.Name ?? "",
                    AbsentCount = absent
                });
            }
        }
        return alerts.OrderByDescending(a => a.AbsentCount).ToList();
    }

    public async Task<List<AttendanceHistoryDto>> GetAttendanceHistoryAsync(int classId)
    {
        var attendances = await _context.Attendances
            .Include(a => a.Enrollment)
            .Where(a => a.Enrollment.ClassId == classId)
            .OrderByDescending(a => a.SlotDate)
            .ThenByDescending(a => a.SessionNumber)
            .ToListAsync();

        // Get distinct dates and sessions, convert to DTO
        var history = attendances
            .GroupBy(a => new { a.SlotDate.Date, a.SessionNumber })
            .Select(g => new AttendanceHistoryDto 
            { 
                Date = g.Key.Date,
                SessionNumber = g.Key.SessionNumber
            })
            .OrderByDescending(h => h.Date)
            .ThenByDescending(h => h.SessionNumber)
            .ToList();

        return history;
    }

    private async Task<int> GetSessionNumberAsync(int classId, DateTime date)
    {
        var count = await _context.Attendances
            .Include(a => a.Enrollment)
            .Where(a => a.Enrollment.ClassId == classId && a.SlotDate.Date < date.Date)
            .Select(a => a.SlotDate.Date)
            .Distinct()
            .CountAsync();
        return count + 1;
    }

    private static List<(int SessionNumber, DateTime Date)> BuildClassSessions(DateTime semesterStart, DateTime semesterEnd, int slotDayOfWeek)
    {
        var targetDay = (DayOfWeek)Math.Clamp(slotDayOfWeek - 1, 0, 6);
        var start = semesterStart.Date;
        var end = semesterEnd.Date;

        var daysUntilTarget = ((int)targetDay - (int)start.DayOfWeek + 7) % 7;
        var first = start.AddDays(daysUntilTarget);

        var sessions = new List<(int SessionNumber, DateTime Date)>();
        var sessionNumber = 1;
        for (var current = first; current <= end; current = current.AddDays(7))
        {
            sessions.Add((sessionNumber++, current));
        }

        return sessions;
    }

    public async Task<(int ClassId, int StudentId, DateTime SlotDate, int SessionNumber)?> GetAttendanceInfoAsync(int attendanceId)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Enrollment)
            .Where(a => a.Id == attendanceId)
            .Select(a => new { a.Enrollment.ClassId, a.Enrollment.StudentId, a.SlotDate, a.SessionNumber })
            .FirstOrDefaultAsync();

        if (attendance == null)
            return null;

        return (attendance.ClassId, attendance.StudentId, attendance.SlotDate, attendance.SessionNumber);
    }
}
