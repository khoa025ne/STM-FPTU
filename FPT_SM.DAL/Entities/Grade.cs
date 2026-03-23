namespace FPT_SM.DAL.Entities;

public class Grade
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? GPA { get; set; }
    // 1=Draft, 2=Published, 3=Re-evaluated
    public int Status { get; set; } = 1;
    public DateTime? PublishedAt { get; set; }
    public bool IsPassed { get; set; } = false;
    public Enrollment Enrollment { get; set; } = null!;
    public AcademicAnalysis? AcademicAnalysis { get; set; }
}
