namespace FPT_SM.DAL.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public DateTime SlotDate { get; set; }
    public int SessionNumber { get; set; }
    // Present, Absent, Late, Excused
    public string Status { get; set; } = "Present";
    public string? Note { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public bool IsEditable => (DateTime.Now - SlotDate).TotalDays <= 14;
}
