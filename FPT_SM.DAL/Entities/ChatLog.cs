namespace FPT_SM.DAL.Entities;

public class ChatLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? SessionId { get; set; }
    public string Message { get; set; } = null!;
    public string Response { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User User { get; set; } = null!;
    public ChatSession? Session { get; set; }
}
