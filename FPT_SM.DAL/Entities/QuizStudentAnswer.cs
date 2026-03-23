namespace FPT_SM.DAL.Entities;

public class QuizStudentAnswer
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public string? SelectedOptionIds { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal ScoreEarned { get; set; } = 0;
    public QuizSubmission Submission { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
}
