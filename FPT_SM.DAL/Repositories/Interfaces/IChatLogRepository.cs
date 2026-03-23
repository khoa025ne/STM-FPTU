using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IChatLogRepository : IGenericRepository<ChatLog>
{
    Task<IEnumerable<ChatLog>> GetByUserAsync(int userId, int limit = 50);
}
