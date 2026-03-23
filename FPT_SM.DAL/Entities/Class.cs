namespace FPT_SM.DAL.Entities;

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    public int SemesterId { get; set; }
    public int SlotId { get; set; }
    public int Capacity { get; set; } = 30;
    public int Enrolled { get; set; } = 0;
    // 0=Cancelled, 1=Open, 2=Ongoing, 3=Ended
    public int Status { get; set; } = 1;
    public string? RoomNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Subject Subject { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Slot Slot { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<AcademicAnalysis> AcademicAnalyses { get; set; } = new List<AcademicAnalysis>();
}
