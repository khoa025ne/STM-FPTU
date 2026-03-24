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
        var activeEnrollments = enrollments.Where(e => e.Status is 2 or 3);
        var cls = await _context.Classes.Include(c => c.Subject).Include(c => c.Semester).Include(c => c.Slot).FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null) return null;

        var sessionNumber = await GetSessionNumberAsync(classId, date);
        var totalSessions = CalculateTotalSessions(cls.Semester, cls.Slot);
        var items = new List<AttendanceDto>();

        foreach (var e in activeEnrollments)
        {
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.EnrollmentId == e.Id && a.SlotDate.Date == date.Date);
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
                IsEditable = attendance == null || attendance.IsEditable
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
        foreach (var item in dto.Items)
        {
            var enrollment = await _enrollmentRepo.GetByIdAsync(item.EnrollmentId);
            if (enrollment == null || enrollment.Status == 5) continue;

            // Check for existing attendance by date AND session number (to prevent duplicates)
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EnrollmentId == item.EnrollmentId 
                    && a.SlotDate.Date == dto.Date.Date 
                    && a.SessionNumber == dto.SessionNumber);

            if (existing != null)
            {
                if (!existing.IsEditable) continue;
                existing.Status = item.Status;
                existing.Note = item.Note;
                _context.Attendances.Update(existing);
            }
            else
            {
                // Before adding new, delete any other records for same date (cleanup duplicates)
                var duplicates = await _context.Attendances
                    .Where(a => a.EnrollmentId == item.EnrollmentId 
                        && a.SlotDate.Date == dto.Date.Date)
                    .ToListAsync();
                
                if (duplicates.Count > 0)
                {
                    _context.Attendances.RemoveRange(duplicates);
                }

                _context.Attendances.Add(new Attendance
                {
                    EnrollmentId = item.EnrollmentId,
                    SlotDate = dto.Date,
                    SessionNumber = dto.SessionNumber,
                    Status = item.Status,
                    Note = item.Note
                });
            }

            // Check if student should fail due to absences
            if (item.Status == "Absent")
            {
                await _context.SaveChangesAsync();
                var absentCount = await _attendanceRepo.CountAbsentAsync(item.EnrollmentId);
                if (absentCount >= 5 && enrollment.Status != 5)
                {
                    enrollment.Status = 5;
                    _context.Enrollments.Update(enrollment);
                }
            }
        }

        await _context.SaveChangesAsync();
        return ServiceResult.Success("Điểm danh đã được lưu thành công!");
    }

    public async Task<ServiceResult> UpdateAttendanceAsync(int attendanceId, string status, string? note)
    {
        var attendance = await _attendanceRepo.GetByIdAsync(attendanceId);
        if (attendance == null) return ServiceResult.Failure("Không tìm thấy bản ghi điểm danh.");
        if (!attendance.IsEditable) return ServiceResult.Failure("Không thể chỉnh sửa điểm danh sau 14 ngày.");

        var oldStatus = attendance.Status;
        attendance.Status = status;
        attendance.Note = note;
        await _attendanceRepo.UpdateAsync(attendance);

        if (status == "Absent" && oldStatus != "Absent")
        {
            var absentCount = await _attendanceRepo.CountAbsentAsync(attendance.EnrollmentId);
            var enrollment = await _enrollmentRepo.GetByIdAsync(attendance.EnrollmentId);
            if (absentCount >= 5 && enrollment != null && enrollment.Status != 5)
            {
                enrollment.Status = 5;
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

    // Calculate total sessions based on semester dates and class schedule
    private int CalculateTotalSessions(Semester? semester, Slot? slot)
    {
        if (semester == null || slot == null) return 20;  // Default to 20

        var startDate = semester.StartDate;
        var endDate = semester.EndDate;
        var dayOfWeek = (DayOfWeek)slot.DayOfWeek;  // 1=Sunday, 2=Monday, etc.

        int sessionCount = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek == dayOfWeek)
            {
                sessionCount++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return Math.Max(sessionCount, 1);
    }
}
