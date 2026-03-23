namespace FPT_SM.DAL.Entities;

public class QuizAnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; } = false;
    public int OrderIndex { get; set; }
    public QuizQuestion Question { get; set; } = null!;
}
