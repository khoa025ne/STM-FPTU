namespace FPT_SM.DAL.Entities;

public class GradeAnalysisTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal MinGPA { get; set; }
    public decimal MaxGPA { get; set; }
    public string TemplateText { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
