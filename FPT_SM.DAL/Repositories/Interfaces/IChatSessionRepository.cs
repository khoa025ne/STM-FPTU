using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IChatSessionRepository
{
    Task<ChatSession> GetByIdAsync(int id);
    Task<List<ChatSession>> GetByUserAsync(int userId, bool includeArchived = false);
    Task<ChatSession> CreateAsync(ChatSession session);
    Task<ChatSession> UpdateAsync(ChatSession session);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
