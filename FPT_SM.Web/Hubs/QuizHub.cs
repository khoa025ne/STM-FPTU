using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Hubs;

public class QuizHub : Hub
{
    public async Task JoinGroup(string groupName) => await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    public async Task LeaveGroup(string groupName) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}
