namespace FPT_SM.DAL.Entities;

public class Prerequisite
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int PrerequisiteSubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public Subject PrerequisiteSubject { get; set; } = null!;
}
