using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IChatbotService
{
    Task<string> ProcessMessageAsync(int userId, string message);
    Task<List<ChatMessageDto>> GetHistoryAsync(int userId, int limit = 50);
}
