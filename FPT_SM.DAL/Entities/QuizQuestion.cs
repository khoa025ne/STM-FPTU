namespace FPT_SM.DAL.Entities;

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuestionText { get; set; } = null!;
    // SingleChoice, MultipleChoice, Essay
    public string QuestionType { get; set; } = null!;
    public decimal Points { get; set; } = 1;
    public int OrderIndex { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizAnswerOption> AnswerOptions { get; set; } = new List<QuizAnswerOption>();
    public ICollection<QuizStudentAnswer> StudentAnswers { get; set; } = new List<QuizStudentAnswer>();
}
