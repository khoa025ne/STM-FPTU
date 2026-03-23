using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.BLL.Services.Implementations;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IClassRepository _classRepo;
    private readonly ISubjectRepository _subjectRepo;
    private readonly IGradeRepository _gradeRepo;
    private readonly IUserRepository _userRepo;
    private readonly AppDbContext _context;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepo,
        IClassRepository classRepo,
        ISubjectRepository subjectRepo,
        IGradeRepository gradeRepo,
        IUserRepository userRepo,
        AppDbContext context)
    {
        _enrollmentRepo = enrollmentRepo;
        _classRepo = classRepo;
        _subjectRepo = subjectRepo;
        _gradeRepo = gradeRepo;
        _userRepo = userRepo;
        _context = context;
    }

    public async Task<List<EnrollmentDto>> GetByStudentAsync(int studentId, int? semesterId = null)
    {
        var enrollments = await _enrollmentRepo.GetByStudentAsync(studentId, semesterId);
        var result = new List<EnrollmentDto>();
        foreach (var e in enrollments)
        {
            var absent = await _context.Attendances.CountAsync(a => a.EnrollmentId == e.Id && a.Status == "Absent");
            result.Add(MapToDto(e, absent));
        }
        return result;
    }

    public async Task<List<EnrollmentDto>> GetByClassAsync(int classId)
    {
        var enrollments = await _enrollmentRepo.GetByClassAsync(classId);
        var result = new List<EnrollmentDto>();
        foreach (var e in enrollments)
        {
            var absent = await _context.Attendances.CountAsync(a => a.EnrollmentId == e.Id && a.Status == "Absent");
            result.Add(MapToDto(e, absent));
        }
        return result;
    }

    public async Task<List<AvailableSubjectDto>> GetAvailableSubjectsAsync(int studentId, int semesterId)
    {
        var allSubjects = await _subjectRepo.GetAllWithPrerequisitesAsync();
        var result = new List<AvailableSubjectDto>();

        foreach (var subject in allSubjects)
        {
            if (await _enrollmentRepo.IsStudentEnrolledInSubjectAsync(studentId, subject.Id, semesterId))
                continue;

            var prereqsMet = true;
            var missing = new List<string>();
            foreach (var prereq in subject.Prerequisites)
            {
                var grade = await _gradeRepo.GetStudentGradeForSubjectAsync(studentId, prereq.PrerequisiteSubjectId);
                if (grade == null || !grade.IsPassed)
                {
                    prereqsMet = false;
                    missing.Add($"{prereq.PrerequisiteSubject?.Code} - {prereq.PrerequisiteSubject?.Name}");
                }
            }

            var availableClasses = await _classRepo.GetBySubjectAndSemesterAsync(subject.Id, semesterId);
            var classDtos = availableClasses
                .Where(c => c.Status == 1 || c.Status == 2)
                .Where(c => c.Enrolled < c.Capacity)
                .Select(c => new ClassDto
                {
                    Id = c.Id, Name = c.Name, SubjectId = c.SubjectId,
                    SubjectCode = c.Subject?.Code ?? "", SubjectName = c.Subject?.Name ?? "",
                    TeacherId = c.TeacherId, TeacherName = c.Teacher?.FullName ?? "",
                    SemesterId = c.SemesterId, SemesterName = c.Semester?.Name ?? "",
                    SlotId = c.SlotId, SlotName = c.Slot?.Name ?? "",
                    DayOfWeek = c.Slot?.DayOfWeek ?? 0,
                    StartTime = c.Slot?.StartTime ?? TimeSpan.Zero,
                    EndTime = c.Slot?.EndTime ?? TimeSpan.Zero,
                    Capacity = c.Capacity, Enrolled = c.Enrolled, Status = c.Status,
                    RoomNumber = c.RoomNumber, CreatedAt = c.CreatedAt
                }).ToList();

            result.Add(new AvailableSubjectDto
            {
                SubjectId = subject.Id,
                SubjectCode = subject.Code,
                SubjectName = subject.Name,
                Credits = subject.Credits,
                Price = subject.Price,
                PrerequisitesMet = prereqsMet,
                MissingPrerequisites = missing,
                AvailableClasses = classDtos
            });
        }
        return result;
    }

    public async Task<ServiceResult> ProcessEnrollmentAsync(int studentId, EnrollRegistrationDto dto)
    {
        var student = await _userRepo.GetByIdAsync(studentId);
        if (student == null) return ServiceResult.Failure("Không tìm thấy sinh viên.");

        // Validate all classes first
        var classesToEnroll = new List<Class>();
        decimal totalCost = 0;

        var existingEnrollments = await _enrollmentRepo.GetByStudentAsync(studentId, dto.SemesterId);
        if (existingEnrollments.Count() + dto.ClassIds.Count > 5)
            return ServiceResult.Failure("Không thể đăng ký quá 5 môn trong một học kỳ.");

        foreach (var classId in dto.ClassIds)
        {
            var cls = await _classRepo.GetWithDetailsAsync(classId);
            if (cls == null) return ServiceResult.Failure($"Không tìm thấy lớp ID={classId}.");
            if (cls.Enrolled >= cls.Capacity) return ServiceResult.Failure($"Lớp {cls.Name} đã hết chỗ.");
            if (cls.Status != 1 && cls.Status != 2) return ServiceResult.Failure($"Lớp {cls.Name} hiện không nhận đăng ký.");

            if (await _enrollmentRepo.IsStudentEnrolledInSubjectAsync(studentId, cls.SubjectId, dto.SemesterId))
                return ServiceResult.Failure($"Bạn đã đăng ký môn {cls.Subject?.Name} trong học kỳ này rồi.");

            if (!await HasPassedPrerequisitesAsync(studentId, cls.SubjectId))
                return ServiceResult.Failure($"Bạn chưa hoàn thành môn tiên quyết của {cls.Subject?.Name}.");

            // Check schedule conflict with already enrolled
            foreach (var existing in classesToEnroll)
            {
                if (existing.SlotId == cls.SlotId)
                    return ServiceResult.Failure($"Lớp {cls.Name} bị trùng lịch với {existing.Name}.");
            }

            classesToEnroll.Add(cls);
            totalCost += cls.Subject?.Price ?? 6000000;
        }

        if (student.WalletBalance < totalCost)
            return ServiceResult.Failure($"Số dư ví không đủ. Cần {totalCost:N0} VNĐ, hiện có {student.WalletBalance:N0} VNĐ.");

        // Process payment and enrollment
        student.WalletBalance -= totalCost;
        await _userRepo.UpdateAsync(student);

        _context.Transactions.Add(new Transaction
        {
            UserId = studentId,
            Amount = totalCost,
            Type = "TuitionPayment",
            Status = "Success",
            Description = $"Thanh toán học phí {classesToEnroll.Count} môn - Kỳ {dto.SemesterId}",
            CreatedAt = DateTime.Now
        });

        foreach (var cls in classesToEnroll)
        {
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                ClassId = cls.Id,
                SemesterId = dto.SemesterId,
                Status = 2,
                EnrolledAt = DateTime.Now
            };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            _context.Grades.Add(new Grade { EnrollmentId = enrollment.Id, Status = 1 });

            for (int i = 1; i <= 20; i++)
            {
                _context.Attendances.Add(new Attendance
                {
                    EnrollmentId = enrollment.Id,
                    SlotDate = DateTime.Now.AddDays(i * 7),
                    SessionNumber = i,
                    Status = "Present"
                });
            }

            cls.Enrolled++;
            _context.Classes.Update(cls);
        }

        await _context.SaveChangesAsync();
        return ServiceResult.Success($"Đăng ký thành công {classesToEnroll.Count} môn học. Đã trừ {totalCost:N0} VNĐ.");
    }

    public async Task<ServiceResult> DropEnrollmentAsync(int enrollmentId, int studentId)
    {
        var enrollment = await _enrollmentRepo.GetWithDetailsAsync(enrollmentId);
        if (enrollment == null) return ServiceResult.Failure("Không tìm thấy đăng ký học.");
        if (enrollment.StudentId != studentId) return ServiceResult.Failure("Không có quyền thực hiện.");
        if (enrollment.Status == 4) return ServiceResult.Failure("Môn học này đã được rút.");

        enrollment.Status = 4;
        enrollment.DroppedAt = DateTime.Now;
        await _enrollmentRepo.UpdateAsync(enrollment);
        return ServiceResult.Success("Đã rút môn học thành công.");
    }

    public async Task<bool> HasPassedPrerequisitesAsync(int studentId, int subjectId)
    {
        var prereqs = await _subjectRepo.GetPrerequisitesAsync(subjectId);
        foreach (var prereq in prereqs)
        {
            var grade = await _gradeRepo.GetStudentGradeForSubjectAsync(studentId, prereq.PrerequisiteSubjectId);
            if (grade == null || !grade.IsPassed) return false;
        }
        return true;
    }

    private static EnrollmentDto MapToDto(Enrollment e, int absentCount) => new()
    {
        Id = e.Id,
        StudentId = e.StudentId,
        StudentName = e.Student?.FullName ?? "",
        StudentRollNumber = e.Student?.RollNumber,
        ClassId = e.ClassId,
        ClassName = e.Class?.Name ?? "",
        SubjectCode = e.Class?.Subject?.Code ?? "",
        SubjectName = e.Class?.Subject?.Name ?? "",
        SemesterId = e.SemesterId,
        SemesterName = e.Semester?.Name ?? "",
        Status = e.Status,
        EnrolledAt = e.EnrolledAt,
        Grade = e.Grade == null ? null : new GradeDto
        {
            Id = e.Grade.Id, EnrollmentId = e.Grade.EnrollmentId,
            MidtermScore = e.Grade.MidtermScore, FinalScore = e.Grade.FinalScore,
            GPA = e.Grade.GPA, Status = e.Grade.Status,
            IsPassed = e.Grade.IsPassed, PublishedAt = e.Grade.PublishedAt
        },
        AbsentCount = absentCount,
        DayName = e.Class?.Slot?.DayOfWeek switch
        {
            2 => "Thứ Hai", 3 => "Thứ Ba", 4 => "Thứ Tư", 5 => "Thứ Năm",
            6 => "Thứ Sáu", 7 => "Thứ Bảy", 1 => "Chủ Nhật", _ => ""
        } ?? "",
        StartTime = e.Class?.Slot?.StartTime ?? TimeSpan.Zero,
        EndTime = e.Class?.Slot?.EndTime ?? TimeSpan.Zero,
        RoomNumber = e.Class?.RoomNumber,
        TeacherName = e.Class?.Teacher?.FullName ?? ""
    };
}
