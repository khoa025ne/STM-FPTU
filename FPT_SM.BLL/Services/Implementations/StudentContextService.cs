using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Repositories.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class StudentContextService : IStudentContextService
{
    private readonly IUserRepository _userRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IGradeRepository _gradeRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IQuizRepository _quizRepo;
    private readonly IClassRepository _classRepo;
    private readonly IEnrollmentService _enrollmentService;

    public StudentContextService(
        IUserRepository userRepo,
        IEnrollmentRepository enrollmentRepo,
        IGradeRepository gradeRepo,
        IAttendanceRepository attendanceRepo,
        ISemesterRepository semesterRepo,
        IQuizRepository quizRepo,
        IClassRepository classRepo,
        IEnrollmentService enrollmentService)
    {
        _userRepo = userRepo;
        _enrollmentRepo = enrollmentRepo;
        _gradeRepo = gradeRepo;
        _attendanceRepo = attendanceRepo;
        _semesterRepo = semesterRepo;
        _quizRepo = quizRepo;
        _classRepo = classRepo;
        _enrollmentService = enrollmentService;
    }

    public async Task<StudentContextDto> GetStudentContextAsync(int userId)
    {
        // Fetch user info
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return new StudentContextDto { UserId = userId, FullName = "Unknown Student" };

        try
        {
            // Fetch current semester
            var currentSemester = await _semesterRepo.GetOngoingSemesterAsync();

            // Get enrollment eligibility (current program semester)
            var eligibility = await _enrollmentService.GetEnrollmentEligibilityAsync(userId);

            // Fetch enrollments
            var enrollments = await _enrollmentRepo.GetByStudentAsync(userId);
            var activeEnrollments = enrollments.Where(e => e.Status == 1 || e.Status == 2).ToList();

            // Build active classes
            var activeClasses = new List<ActiveClassDto>();
            foreach (var enrollment in activeEnrollments)
            {
                var cls = await _classRepo.GetWithDetailsAsync(enrollment.ClassId);
                if (cls != null)
                {
                    activeClasses.Add(new ActiveClassDto
                    {
                        ClassName = cls.Name ?? "Unknown",
                        SubjectName = cls.Subject?.Name ?? "Unknown",
                        TeacherName = cls.Teacher?.FullName ?? "Unknown"
                    });
                }
            }

            // Fetch grades
            var grades = await _gradeRepo.GetByStudentAsync(userId);
            var recentGrades = grades
                .OrderByDescending(g => g.Id)
                .Take(5)
                .Select(g => new GradeInfoDto
                {
                    SubjectName = g.Enrollment?.Class?.Subject?.Name ?? "Unknown",
                    MidtermScore = g.MidtermScore,
                    FinalScore = g.FinalScore,
                    GPA = g.GPA
                })
                .ToList();

            // Calculate average GPA
            var averageGPA = recentGrades
                .Where(g => g.GPA.HasValue)
                .Select(g => (double)g.GPA.Value)
                .DefaultIfEmpty(0)
                .Average();

            // Fetch attendance summaries
            int totalAbsences = 0;
            int totalLate = 0;
            foreach (var enrollment in activeEnrollments)
            {
                var attendances = await _attendanceRepo.GetByEnrollmentAsync(enrollment.Id);
                totalAbsences += attendances.Count(a => a.Status == "Absent");
                totalLate += attendances.Count(a => a.Status == "Late");
            }

            // Fetch upcoming quizzes for active classes
            var upcomingQuizzes = new List<UpcomingQuizDto>();
            if (activeEnrollments.Any())
            {
                try
                {
                    var allQuizzes = await _quizRepo.GetActiveForStudentAsync(userId);
                    upcomingQuizzes = allQuizzes
                        .Where(q => q.StartTime > DateTime.Now)
                        .OrderBy(q => q.StartTime)
                        .Take(5)
                        .Select(q => new UpcomingQuizDto
                        {
                            Title = q.Title ?? "Quiz",
                            StartTime = q.StartTime,
                            ClassName = q.Class?.Name ?? "Unknown"
                        })
                        .ToList();
                }
                catch
                {
                    // If quiz retrieval fails, continue without quiz data
                    upcomingQuizzes = new List<UpcomingQuizDto>();
                }
            }

            return new StudentContextDto
            {
                UserId = userId,
                FullName = user.FullName ?? "Student",
                RollNumber = user.RollNumber ?? "N/A",
                WalletBalance = user.WalletBalance,
                CurrentSemester = currentSemester?.Name ?? "N/A",
                CurrentProgramSemester = eligibility.PayableProgramSemester,
                ActiveClasses = activeClasses,
                RecentGrades = recentGrades,
                AttendanceSummary = new AttendanceSummaryDto
                {
                    TotalAbsences = totalAbsences,
                    TotalLate = totalLate
                },
                UpcomingQuizzes = upcomingQuizzes,
                TotalAbsences = totalAbsences,
                AverageGPA = averageGPA > 0 ? averageGPA : null
            };
        }
        catch
        {
            // Fallback: return basic context if any query fails
            return new StudentContextDto
            {
                UserId = userId,
                FullName = user.FullName ?? "Student",
                RollNumber = user.RollNumber ?? "N/A",
                WalletBalance = user.WalletBalance,
                CurrentSemester = "N/A"
            };
        }
    }
}
