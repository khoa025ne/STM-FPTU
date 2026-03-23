namespace FPT_SM.DAL.Entities;

public class Subject
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Credits { get; set; } = 3;
    public decimal Price { get; set; } = 6000000;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Prerequisite> Prerequisites { get; set; } = new List<Prerequisite>();
    public ICollection<Prerequisite> IsPrerequisiteFor { get; set; } = new List<Prerequisite>();
}
