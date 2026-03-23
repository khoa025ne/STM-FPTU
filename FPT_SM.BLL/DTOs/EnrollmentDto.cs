namespace FPT_SM.BLL.DTOs;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentRollNumber { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = null!;
    public int Status { get; set; }
    public string StatusName => Status switch
    {
        1 => "Đã đăng ký",2 => "Đã thanh toán",3 => "Hoàn thành",
        4 => "Đã rút",5 => "Rớt môn",_ => "Không xác định"
    };
    public DateTime EnrolledAt { get; set; }
    public GradeDto? Grade { get; set; }
    public int AbsentCount { get; set; }
    public bool IsFailedByAbsence => AbsentCount >= 5;
    public string DayName { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? RoomNumber { get; set; }
    public string TeacherName { get; set; } = null!;
}

public class EnrollRegistrationDto
{
    public int SemesterId { get; set; }
    public List<int> ClassIds { get; set; } = new();
}

public class AvailableSubjectDto
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public bool PrerequisitesMet { get; set; }
    public List<string> MissingPrerequisites { get; set; } = new();
    public List<ClassDto> AvailableClasses { get; set; } = new();
}
