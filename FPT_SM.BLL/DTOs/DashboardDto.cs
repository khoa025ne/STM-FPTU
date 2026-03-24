namespace FPT_SM.BLL.DTOs;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int ActiveSemesters { get; set; }
    public int TotalClasses { get; set; }
    public int OngoingClasses { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<SemesterDto> RecentSemesters { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    public List<MonthlyRevenueDto> RevenueChart { get; set; } = new();
}

public class ManagerDashboardDto
{
    public int PendingClasses { get; set; }
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int TotalEnrollments { get; set; }
    public double PassRate { get; set; }
    public List<ClassDto> ClassesPendingApproval { get; set; } = new();
    public List<SemesterDto> ActiveSemesters { get; set; } = new();
}

public class TeacherDashboardDto
{
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int ClassesToday { get; set; }
    public int StudentsNeedingAttention { get; set; }
    public int UnpublishedGrades { get; set; }
    public int ActiveQuizzes { get; set; }
    public List<ClassDto> MyClasses { get; set; } = new();
    public List<AttendanceAlertDto> AttendanceAlerts { get; set; } = new();
}

public class StudentDashboardDto
{
    public int TotalEnrollments { get; set; }
    public int CurrentSemesterSubjects { get; set; }
    public decimal AverageGPA { get; set; }
    public int TotalAbsences { get; set; }
    public decimal WalletBalance { get; set; }
    public int NewNotifications { get; set; }
    public int ActiveQuizzes { get; set; }
    public int CurrentProgramSemester { get; set; } = 1;
    public List<EnrollmentDto> CurrentEnrollments { get; set; } = new();
    public List<QuizDto> UpcomingQuizzes { get; set; } = new();
    public SemesterDto? CurrentSemester { get; set; }
}

public class AttendanceAlertDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? RollNumber { get; set; }
    public string ClassName { get; set; } = null!;
    public int AbsentCount { get; set; }
    public bool IsFailed => AbsentCount >= 5;
}

public class NotificationDto
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = "info";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public object? Data { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = null!;
    public decimal Revenue { get; set; }
}

public class AcademicAnalysisDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public string AnalysisText { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class AnalyzeGradeDto
{
    public int GradeId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? RollNumber { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public decimal? Midterm { get; set; }
    public decimal? Final { get; set; }
    public decimal? GPA { get; set; }
    public bool IsPassed { get; set; }
    public int ClassId { get; set; }
    public int SemesterId { get; set; }
    public int TeacherId { get; set; }
}
