using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class ChatSessionRepository : GenericRepository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(AppDbContext context) : base(context) { }

    public async Task<ChatSession> GetByIdAsync(int id)
    {
        return await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id) ?? new ChatSession();
    }

    public async Task<List<ChatSession>> GetByUserAsync(int userId, bool includeArchived = false)
    {
        var query = _context.ChatSessions
            .Where(s => s.UserId == userId);

        if (!includeArchived)
            query = query.Where(s => !s.IsArchived);

        return await query
            .OrderByDescending(s => s.UpdatedAt)
            .Include(s => s.Messages)
            .ToListAsync();
    }

    public async Task<ChatSession> CreateAsync(ChatSession session)
    {
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<ChatSession> UpdateAsync(ChatSession session)
    {
        session.UpdatedAt = DateTime.Now;
        _context.ChatSessions.Update(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task DeleteAsync(int id)
    {
        var session = await _context.ChatSessions.FindAsync(id);
        if (session != null)
        {
            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.ChatSessions.AnyAsync(s => s.Id == id);
    }
}
