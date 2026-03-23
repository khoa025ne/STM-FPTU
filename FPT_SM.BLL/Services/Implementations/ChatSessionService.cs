using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class ChatSessionService : IChatSessionService
{
    private readonly IChatSessionRepository _sessionRepo;

    public ChatSessionService(IChatSessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public async Task<List<string>> GetUserSessionsAsync(int userId)
    {
        try
        {
            var sessions = await _sessionRepo.GetByUserAsync(userId, includeArchived: false);
            return sessions.Select(s => $"{s.Id}|{s.Title}").ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<int> CreateSessionAsync(int userId, string title)
    {
        try
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = title ?? "Chat mới",
                Topic = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var created = await _sessionRepo.CreateAsync(session);
            return created.Id;
        }
        catch
        {
            return 0;
        }
    }

    public async Task UpdateSessionAsync(int sessionId, string newTitle)
    {
        try
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session.Id > 0)
            {
                session.Title = newTitle;
                await _sessionRepo.UpdateAsync(session);
            }
        }
        catch { }
    }

    public async Task DeleteSessionAsync(int sessionId)
    {
        try
        {
            await _sessionRepo.DeleteAsync(sessionId);
        }
        catch { }
    }

    public async Task SetActiveSessionAsync(int userId, int sessionId)
    {
        // Store in session cache or local storage (handled client-side)
        // This can be enhanced with database or cache later
        await Task.CompletedTask;
    }

    public async Task<int?> GetActiveSessionAsync(int userId)
    {
        // Retrieve from session cache (handled client-side)
        return await Task.FromResult<int?>(null);
    }
}
