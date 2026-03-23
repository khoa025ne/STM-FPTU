namespace FPT_SM.DAL.Entities;

/// <summary>
/// Represents a chat session - a collection of messages grouped by topic
/// </summary>
public class ChatSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Topic { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ChatLog> Messages { get; set; } = new List<ChatLog>();
}
