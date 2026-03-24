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

    public async Task<AttendanceSheetDto?> GetAttendanceSheetAsync(int classId, DateTime date)
    {
        var enrollments = await _enrollmentRepo.GetByClassAsync(classId);
        var activeEnrollments = enrollments.Where(e => e.Status is 2 or 3).ToList();
        var cls = await _context.Classes.Include(c => c.Subject).Include(c => c.Semester).FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null) return null;

        // Validate date - use today if invalid or default date (0001-01-01)
        if (date.Year <= 1)
        {
            date = DateTime.Today;
        }
        
        // If semester exists, ensure date is within semester bounds
        if (cls.Semester != null)
        {
            if (date < cls.Semester.StartDate)
                date = cls.Semester.StartDate;
            else if (date > cls.Semester.EndDate)
                date = cls.Semester.EndDate;
        }

        // Calculate session number based on distinct dates before this date
        var sessionNumber = await GetSessionNumberAsync(classId, date);
        
        // Total sessions should be based on the number of weeks in semester
        // If semester is defined, calculate based on semester duration, otherwise default to 20
        int totalSessions = 20; // Default
        if (cls.Semester != null)
        {
            var semesterDays = (cls.Semester.EndDate.Date - cls.Semester.StartDate.Date).Days;
            // Assuming 1 session per week (divide by 7)
            totalSessions = Math.Max(1, (semesterDays / 7) + 1);
        }

        var items = new List<AttendanceDto>();

        foreach (var e in activeEnrollments)
        {
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => 
                a.EnrollmentId == e.Id && a.SlotDate.Date == date.Date && a.SessionNumber == sessionNumber);
            
            var absentCount = await _attendanceRepo.CountAbsentAsync(e.Id);
            items.Add(new AttendanceDto
            {
                Id = attendance?.Id ?? 0,
                EnrollmentId = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student?.FullName ?? "",
                StudentRollNumber = e.Student?.RollNumber,
                SlotDate = date,
                SessionNumber = sessionNumber,
                Status = attendance?.Status ?? "Present",
                Note = attendance?.Note,
                IsEditable = attendance == null || (DateTime.Now - attendance.SlotDate).TotalDays <= 14
            });
        }

        return new AttendanceSheetDto
        {
            ClassId = classId,
            ClassName = cls.Name,
            SubjectName = cls.Subject?.Name ?? "",
            Date = date,
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

        foreach (var e in enrollments.Where(e => e.Status != 4))
        {
            var attendances = await _attendanceRepo.GetByEnrollmentAsync(e.Id);
            var dtoList = attendances.Select(a => new AttendanceDto
            {
                Id = a.Id, EnrollmentId = a.EnrollmentId, SlotDate = a.SlotDate,
                SessionNumber = a.SessionNumber, Status = a.Status, Note = a.Note, IsEditable = a.IsEditable
            }).ToList();

            result.Add(new StudentAttendanceSummaryDto
            {
                EnrollmentId = e.Id,
                SubjectName = e.Class?.Subject?.Name ?? "",
                SubjectCode = e.Class?.Subject?.Code ?? "",
                TotalSessions = dtoList.Count,
                PresentCount = dtoList.Count(a => a.Status == "Present"),
                AbsentCount = dtoList.Count(a => a.Status == "Absent"),
                LateCount = dtoList.Count(a => a.Status == "Late"),
                ExcusedCount = dtoList.Count(a => a.Status == "Excused"),
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
        var dtoList = attendances.Select(a => new AttendanceDto
        {
            Id = a.Id, EnrollmentId = a.EnrollmentId, SlotDate = a.SlotDate,
            SessionNumber = a.SessionNumber, Status = a.Status, Note = a.Note, IsEditable = a.IsEditable
        }).ToList();

        return new StudentAttendanceSummaryDto
        {
            EnrollmentId = enrollmentId,
            SubjectName = enrollment.Class?.Subject?.Name ?? "",
            SubjectCode = enrollment.Class?.Subject?.Code ?? "",
            TotalSessions = dtoList.Count,
            PresentCount = dtoList.Count(a => a.Status == "Present"),
            AbsentCount = dtoList.Count(a => a.Status == "Absent"),
            LateCount = dtoList.Count(a => a.Status == "Late"),
            ExcusedCount = dtoList.Count(a => a.Status == "Excused"),
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

    public async Task<(int ClassId, DateTime SlotDate)?> GetAttendanceInfoAsync(int attendanceId)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Enrollment)
            .Where(a => a.Id == attendanceId)
            .Select(a => new { a.Enrollment.ClassId, a.SlotDate })
            .FirstOrDefaultAsync();

        if (attendance == null)
            return null;

        return (attendance.ClassId, attendance.SlotDate);
    }
}
