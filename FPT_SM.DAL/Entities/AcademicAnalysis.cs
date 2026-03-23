namespace FPT_SM.DAL.Entities;

public class AcademicAnalysis
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ClassId { get; set; }
    public int SemesterId { get; set; }
    public int? GradeId { get; set; }
    public string AnalysisText { get; set; } = null!;
    public string? AnalysisJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int TriggeredByTeacherId { get; set; }
    public User Student { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Grade? Grade { get; set; }
}
