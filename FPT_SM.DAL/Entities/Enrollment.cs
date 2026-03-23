namespace FPT_SM.DAL.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ClassId { get; set; }
    public int SemesterId { get; set; }
    // 1=Enrolled, 2=Paid, 3=Completed, 4=Dropped, 5=Failed
    public int Status { get; set; } = 1;
    public DateTime EnrolledAt { get; set; } = DateTime.Now;
    public DateTime? DroppedAt { get; set; }
    public User Student { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Grade? Grade { get; set; }
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
