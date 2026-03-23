namespace FPT_SM.BLL.DTOs;

public class GradeDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentRollNumber { get; set; }
    public int ClassId { get; set; }
    public int SemesterId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? GPA { get; set; }
    public int Status { get; set; }
    public string StatusName => Status switch
    {
        1 => "Nháp",2 => "Đã công bố",3 => "Đánh giá lại",_ => "Không xác định"
    };
    public DateTime? PublishedAt { get; set; }
    public bool IsPassed { get; set; }
    public string ResultText => IsPassed ? "ĐẠT" : "KHÔNG ĐẠT";
    public string ResultColor => IsPassed ? "teal" : "red";
    public string? AIAnalysis { get; set; }
}

public class UpdateGradeDto
{
    public int GradeId { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
}

public class GradeSheetDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public List<GradeDto> Grades { get; set; } = new();
}
