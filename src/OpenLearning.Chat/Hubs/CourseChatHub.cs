using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Services;

namespace OpenLearning.Chat.Hubs;

[Authorize]
public class CourseChatHub : Hub
{
    private readonly ChatService _chat;
    private readonly UserManager<ApplicationUser> _userManager;

    public CourseChatHub(ChatService chat, UserManager<ApplicationUser> userManager)
    {
        _chat = chat;
        _userManager = userManager;
    }

    public async Task JoinCourse(int courseId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || await IsSuspendedAsync())
        {
            return;
        }

        if (!await _chat.IsParticipantAsync(userId, courseId))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(courseId));
    }

    public async Task SendMessage(int courseId, string body)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || await IsSuspendedAsync())
        {
            return;
        }

        var message = await _chat.AddMessageAsync(courseId, userId, body);
        if (message is null)
        {
            return;
        }

        await Clients.Group(GroupName(courseId)).SendAsync(
            "ReceiveMessage",
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

    private static string GroupName(int courseId)
    {
        return $"course-{courseId}";
    }
}
