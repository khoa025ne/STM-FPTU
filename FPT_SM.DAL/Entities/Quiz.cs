namespace FPT_SM.DAL.Entities;

public class Quiz
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public bool IsPublishResult { get; set; } = false;
    // Midterm, Final, Practice
    public string? QuizType { get; set; }
    // 1=Draft, 2=Published
    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Class Class { get; set; } = null!;
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizSubmission> Submissions { get; set; } = new List<QuizSubmission>();
}
