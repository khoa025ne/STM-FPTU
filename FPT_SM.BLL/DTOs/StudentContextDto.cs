namespace FPT_SM.BLL.DTOs;

public class StudentContextDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string RollNumber { get; set; } = null!;
    public decimal WalletBalance { get; set; }
    public string CurrentSemester { get; set; } = "N/A";

    public List<ActiveClassDto> ActiveClasses { get; set; } = new();
    public List<GradeInfoDto> RecentGrades { get; set; } = new();
    public AttendanceSummaryDto AttendanceSummary { get; set; } = new();
    public List<UpcomingQuizDto> UpcomingQuizzes { get; set; } = new();
    public int TotalAbsences { get; set; }
    public double? AverageGPA { get; set; }
}

public class ActiveClassDto
{
    public string ClassName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string TeacherName { get; set; } = null!;
}

public class GradeInfoDto
{
    public string SubjectName { get; set; } = null!;
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? GPA { get; set; }
}

public class AttendanceSummaryDto
{
    public int TotalAbsences { get; set; }
    public int TotalLate { get; set; }
}

public class UpcomingQuizDto
{
    public string Title { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public string ClassName { get; set; } = null!;
}
