using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OpenLearning.Auth.Models;
using OpenLearning.Live.Services;

namespace OpenLearning.Live.Hubs;

/// <summary>Per-session live chat hub. Messages persist as Course-chat records scoped by SessionId.</summary>
[Authorize]
public class LiveChatHub : Hub
{
    private readonly LiveService _live;
    private readonly UserManager<ApplicationUser> _userManager;

    public LiveChatHub(LiveService live, UserManager<ApplicationUser> userManager)
    {
        _live = live;
        _userManager = userManager;
    }

    public async Task JoinLive(int sessionId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || await IsSuspendedAsync())
        {
            return;
        }

        if (!await _live.CanAccessAsync(sessionId, userId, isAdmin: false))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public async Task SendLiveMessage(int sessionId, string body)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || await IsSuspendedAsync())
        {
            return;
        }

        var message = await _live.AddLiveMessageAsync(sessionId, userId, body);
        if (message is null)
        {
            return;
        }

        await Clients.Group(GroupName(sessionId)).SendAsync(
            "ReceiveLiveMessage",
            message.User?.DisplayName ?? userId,
            message.Body,
            message.SentAt);
    }

    private async Task<bool> IsSuspendedAsync()
    {
        if (Context.User is null)
        {
            return true;
        }

        var user = await _userManager.GetUserAsync(Context.User);
        return user?.IsSuspended == true;
    }

    private static string GroupName(int sessionId)
    {
        return $"live-{sessionId}";
    }
}
