namespace FPT_SM.DAL.Entities;

public class QuizSubmission
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public int StudentId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? SubmittedAt { get; set; }
    public decimal TotalScore { get; set; } = 0;
    // InProgress, Submitted, Graded
    public string Status { get; set; } = "InProgress";
    public Quiz Quiz { get; set; } = null!;
    public User Student { get; set; } = null!;
    public ICollection<QuizStudentAnswer> StudentAnswers { get; set; } = new List<QuizStudentAnswer>();
}
