using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class ChatLogRepository : GenericRepository<ChatLog>, IChatLogRepository
{
    public ChatLogRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ChatLog>> GetByUserAsync(int userId, int limit = 50)
        => await _context.ChatLogs
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
}
