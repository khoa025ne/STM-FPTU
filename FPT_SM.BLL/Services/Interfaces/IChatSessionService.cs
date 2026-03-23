namespace FPT_SM.BLL.Services.Interfaces;

public interface IChatSessionService
{
    Task<List<string>> GetUserSessionsAsync(int userId);
    Task<int> CreateSessionAsync(int userId, string title);
    Task UpdateSessionAsync(int sessionId, string newTitle);
    Task DeleteSessionAsync(int sessionId);
    Task SetActiveSessionAsync(int userId, int sessionId);
    Task<int?> GetActiveSessionAsync(int userId);
}
