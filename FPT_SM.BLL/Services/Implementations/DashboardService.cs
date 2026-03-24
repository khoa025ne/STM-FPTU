using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.BLL.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUserRepository _userRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IClassRepository _classRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IGradeRepository _gradeRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IQuizRepository _quizRepo;
    private readonly IEnrollmentService _enrollmentService;
    private readonly AppDbContext _context;

    public DashboardService(
        IUserRepository userRepo,
        ISemesterRepository semesterRepo,
        IClassRepository classRepo,
        IEnrollmentRepository enrollmentRepo,
        IGradeRepository gradeRepo,
        ITransactionRepository transactionRepo,
        IAttendanceRepository attendanceRepo,
        IQuizRepository quizRepo,
        IEnrollmentService enrollmentService,
        AppDbContext context)
    {
        _userRepo = userRepo;
        _semesterRepo = semesterRepo;
        _classRepo = classRepo;
        _enrollmentRepo = enrollmentRepo;
        _gradeRepo = gradeRepo;
        _transactionRepo = transactionRepo;
        _attendanceRepo = attendanceRepo;
        _quizRepo = quizRepo;
        _enrollmentService = enrollmentService;
        _context = context;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var totalUsers = await _userRepo.CountAsync();
        var totalStudents = await _userRepo.CountAsync(u => u.RoleId == 4);
        var totalTeachers = await _userRepo.CountAsync(u => u.RoleId == 3);
        var allClasses = await _classRepo.GetAllWithDetailsAsync();
        var classlist = allClasses.ToList();
        var ongoingClasses = classlist.Count(c => c.Status == 2);
        var totalClasses = classlist.Count;
        var totalEnrollments = await _enrollmentRepo.CountAsync();
        var activeSemesters = await _semesterRepo.CountAsync(s => s.Status == 2);

        var totalRevenue = await _transactionRepo.GetTotalRevenueAsync();
        var monthlyRevenue = await _transactionRepo.GetTotalRevenueAsync(DateTime.Now.Month, DateTime.Now.Year);

        var recentSemesters = (await _semesterRepo.GetOrderedAsync()).Take(5)
            .Select(s => new SemesterDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status
            }).ToList();

        var recentTransactions = (await _context.Transactions
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync())
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User?.FullName ?? "",
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                Description = t.Description,
                VnPayTransactionId = t.VnPayTransactionId,
                OrderId = t.OrderId,
                CreatedAt = t.CreatedAt
            }).ToList();

        // Revenue chart last 12 months
        var revenueChart = new List<MonthlyRevenueDto>();
        for (int i = 11; i >= 0; i--)
        {
            var date = DateTime.Now.AddMonths(-i);
            var rev = await _transactionRepo.GetTotalRevenueAsync(date.Month, date.Year);
            revenueChart.Add(new MonthlyRevenueDto
            {
                Month = date.ToString("MM/yyyy"),
                Revenue = rev
            });
        }

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            ActiveSemesters = activeSemesters,
            TotalClasses = totalClasses,
            OngoingClasses = ongoingClasses,
            TotalEnrollments = totalEnrollments,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            RecentSemesters = recentSemesters,
            RecentTransactions = recentTransactions,
            RevenueChart = revenueChart
        };
    }

    public async Task<ManagerDashboardDto> GetManagerDashboardAsync()
    {
        var allClasses = (await _classRepo.GetAllWithDetailsAsync()).ToList();
        var pendingClasses = allClasses.Where(c => c.Status == 1).ToList();
        var totalStudents = await _userRepo.CountAsync(u => u.RoleId == 4);
        var totalEnrollments = await _enrollmentRepo.CountAsync();

        var publishedGrades = await _context.Grades.Where(g => g.Status == 2).ToListAsync();
        int passCount = publishedGrades.Count(g => g.IsPassed);
        double passRate = publishedGrades.Any() ? (double)passCount / publishedGrades.Count * 100 : 0;

        var activeSemesters = (await _semesterRepo.GetOrderedAsync())
            .Where(s => s.Status == 2)
            .Select(s => new SemesterDto
            {
                Id = s.Id, Name = s.Name, Code = s.Code,
                StartDate = s.StartDate, EndDate = s.EndDate, Status = s.Status
            }).ToList();

        return new ManagerDashboardDto
        {
            PendingClasses = pendingClasses.Count,
            TotalClasses = allClasses.Count,
            TotalStudents = totalStudents,
            TotalEnrollments = totalEnrollments,
            PassRate = passRate,
            ClassesPendingApproval = pendingClasses.Take(10).Select(MapClassToDto).ToList(),
            ActiveSemesters = activeSemesters
        };
    }

    public async Task<TeacherDashboardDto> GetTeacherDashboardAsync(int teacherId)
    {
        var myClasses = (await _classRepo.GetByTeacherAsync(teacherId)).ToList();
        var today = DateTime.Now.DayOfWeek;

        var classesToday = myClasses.Count(c =>
            c.Slot != null && (int)today + 1 == c.Slot.DayOfWeek && c.Status == 2);

        var totalStudents = myClasses.Sum(c => c.Enrolled);

        // Students needing attention: enrolled students with 3+ absences
        var studentsNeedingAttention = 0;
        foreach (var cls in myClasses.Where(c => c.Status == 2))
        {
            var enrollments = await _enrollmentRepo.GetByClassAsync(cls.Id);
            foreach (var e in enrollments)
            {
                var absent = await _attendanceRepo.CountAbsentAsync(e.Id);
                if (absent >= 3) studentsNeedingAttention++;
            }
        }

        // Unpublished grades in my classes
        var unpublishedGrades = await _context.Grades
            .Include(g => g.Enrollment)
            .CountAsync(g => myClasses.Select(c => c.Id).Contains(g.Enrollment.ClassId) && g.Status == 1);

        // Active quizzes
        var activeQuizzes = await _context.Quizzes
            .CountAsync(q => myClasses.Select(c => c.Id).Contains(q.ClassId)
                && q.Status == 2
                && q.StartTime <= DateTime.Now
                && q.EndTime >= DateTime.Now);

        // Build alert list
        var alerts = new List<AttendanceAlertDto>();
        foreach (var cls in myClasses.Where(c => c.Status == 2))
        {
            var enrollments = await _enrollmentRepo.GetByClassAsync(cls.Id);
            foreach (var e in enrollments.Where(e => e.Status is 2 or 3))
            {
                var absent = await _attendanceRepo.CountAbsentAsync(e.Id);
                if (absent >= 3)
                    alerts.Add(new AttendanceAlertDto
                    {
                        StudentId = e.StudentId,
                        StudentName = e.Student?.FullName ?? "",
                        RollNumber = e.Student?.RollNumber,
                        ClassName = cls.Name,
                        AbsentCount = absent
                    });
            }
        }

        return new TeacherDashboardDto
        {
            TotalClasses = myClasses.Count,
            TotalStudents = totalStudents,
            ClassesToday = classesToday,
            StudentsNeedingAttention = studentsNeedingAttention,
            UnpublishedGrades = unpublishedGrades,
            ActiveQuizzes = activeQuizzes,
            MyClasses = myClasses.Take(10).Select(MapClassToDto).ToList(),
            AttendanceAlerts = alerts.Take(20).ToList()
        };
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId)
    {
        var currentSemester = await _semesterRepo.GetOngoingSemesterAsync();
        var enrollments = await _enrollmentRepo.GetByStudentAsync(studentId,
            currentSemester?.Id);

        // Get enrollment eligibility (current program semester)
        var eligibility = await _enrollmentService.GetEnrollmentEligibilityAsync(studentId);

        var enrollmentDtos = new List<EnrollmentDto>();
        int totalAbsences = 0;

        foreach (var e in enrollments)
        {
            var absent = await _attendanceRepo.CountAbsentAsync(e.Id);
            totalAbsences += absent;
            enrollmentDtos.Add(new EnrollmentDto
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
                ProgramSemester = e.Class?.Subject?.ProgramSemester,
                Status = e.Status,
                EnrolledAt = e.EnrolledAt,
                AbsentCount = absent,
                DayName = "",
                TeacherName = e.Class?.Teacher?.FullName ?? "",
                Grade = e.Grade == null ? null : new GradeDto
                {
                    Id = e.Grade.Id,
                    EnrollmentId = e.Grade.EnrollmentId,
                    MidtermScore = e.Grade.MidtermScore,
                    FinalScore = e.Grade.FinalScore,
                    GPA = e.Grade.GPA,
                    Status = e.Grade.Status,
                    IsPassed = e.Grade.IsPassed
                }
            });
        }

        // Average GPA
        var grades = enrollmentDtos
            .Where(e => e.Grade?.GPA.HasValue == true)
            .Select(e => e.Grade!.GPA!.Value)
            .ToList();
        var avgGpa = grades.Any() ? grades.Average() : 0;

        var user = await _userRepo.GetByIdAsync(studentId);
        var walletBalance = user?.WalletBalance ?? 0;

        // Active quizzes for student
        var activeQuizzes = await _quizRepo.GetActiveForStudentAsync(studentId);
        var quizDtos = activeQuizzes.Select(q => new QuizDto
        {
            Id = q.Id,
            ClassId = q.ClassId,
            ClassName = q.Class?.Name ?? "",
            SubjectName = q.Class?.Subject?.Name ?? "",
            Title = q.Title,
            Description = q.Description,
            StartTime = q.StartTime,
            EndTime = q.EndTime,
            DurationMinutes = q.DurationMinutes,
            IsPublishResult = q.IsPublishResult,
            QuizType = q.QuizType,
            Status = q.Status,
            QuestionCount = q.Questions?.Count ?? 0
        }).ToList();

        var semesterDto = currentSemester == null ? null : new SemesterDto
        {
            Id = currentSemester.Id,
            Name = currentSemester.Name,
            Code = currentSemester.Code,
            StartDate = currentSemester.StartDate,
            EndDate = currentSemester.EndDate,
            Status = currentSemester.Status
        };

        return new StudentDashboardDto
        {
            TotalEnrollments = enrollmentDtos.Count,
            CurrentSemesterSubjects = enrollmentDtos.Count,
            AverageGPA = (decimal)avgGpa,
            TotalAbsences = totalAbsences,
            WalletBalance = walletBalance,
            NewNotifications = 0,
            ActiveQuizzes = quizDtos.Count,
            CurrentProgramSemester = eligibility.PayableProgramSemester,
            CurrentEnrollments = enrollmentDtos,
            UpcomingQuizzes = quizDtos,
            CurrentSemester = semesterDto
        };
    }

    private ClassDto MapClassToDto(DAL.Entities.Class c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        SubjectId = c.SubjectId,
        SubjectCode = c.Subject?.Code ?? "",
        SubjectName = c.Subject?.Name ?? "",
        TeacherId = c.TeacherId,
        TeacherName = c.Teacher?.FullName ?? "",
        SemesterId = c.SemesterId,
        SemesterName = c.Semester?.Name ?? "",
        SlotId = c.SlotId,
        SlotName = c.Slot?.Name ?? "",
        DayOfWeek = c.Slot?.DayOfWeek ?? 0,
        StartTime = c.Slot?.StartTime ?? TimeSpan.Zero,
        EndTime = c.Slot?.EndTime ?? TimeSpan.Zero,
        Capacity = c.Capacity,
        Enrolled = c.Enrolled,
        Status = c.Status,
        RoomNumber = c.RoomNumber,
        CreatedAt = c.CreatedAt
    };
}
