namespace FPT_SM.BLL.DTOs;

public class ChatMessageDto
{
    public string Message { get; set; } = null!;
    public string Response { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsUser { get; set; }
}

public class SendMessageDto
{
    public string Message { get; set; } = null!;
}
