namespace FPT_SM.DAL.Entities;

public class Semester
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    // 0=Closed, 1=Upcoming, 2=Ongoing, 3=Finished
    public int Status { get; set; } = 1;
    public DateTime? DeletedAt { get; set; } = null; // Soft delete
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<AcademicAnalysis> AcademicAnalyses { get; set; } = new List<AcademicAnalysis>();
}
