namespace FPT_SM.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string? RollNumber { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int RoleId { get; set; }
    public decimal WalletBalance { get; set; } = 0;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Role Role { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<ChatLog> ChatLogs { get; set; } = new List<ChatLog>();
    public ICollection<Class> TeachingClasses { get; set; } = new List<Class>();
    public ICollection<QuizSubmission> QuizSubmissions { get; set; } = new List<QuizSubmission>();
    public ICollection<AcademicAnalysis> AcademicAnalyses { get; set; } = new List<AcademicAnalysis>();
}
