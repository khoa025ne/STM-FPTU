namespace FPT_SM.BLL.DTOs;

public class ReportFilterDto
{
    public int? SemesterId { get; set; }
    public int? SubjectId { get; set; }
    public int? ClassId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}

public class EnrollmentReportDto
{
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public int TotalEnrolled { get; set; }
    public int Capacity { get; set; }
    public double FillRate => Capacity > 0 ? (double)TotalEnrolled / Capacity * 100 : 0;
}

public class GradeReportDto
{
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public int TotalStudents { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public double PassRate => TotalStudents > 0 ? (double)PassCount / TotalStudents * 100 : 0;
    public decimal? AverageGPA { get; set; }
}

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TuitionRevenue { get; set; }
    public decimal DepositRevenue { get; set; }
    public int TransactionCount { get; set; }
    public List<MonthlyRevenueDto> Monthly { get; set; } = new();
}

public class AdminOverviewDto
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
}

public class SemesterEnrollmentDto
{
    public string SemesterName { get; set; } = null!;
    public int EnrollmentCount { get; set; }
}

public class ManagerClassStatusDto
{
    public int Status { get; set; }
    public string StatusLabel { get; set; } = null!;
    public int Count { get; set; }
}

public class ManagerTeacherReportDto
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public int ClassCount { get; set; }
}

public class ClassExportDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string TeacherName { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;
    public int Capacity { get; set; }
    public int Enrolled { get; set; }
    public DateTime CreatedAt { get; set; }
}
