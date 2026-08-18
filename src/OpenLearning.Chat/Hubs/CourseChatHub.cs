using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenLearning.Chat.Services;

namespace OpenLearning.Chat.Hubs;

[Authorize]
public class CourseChatHub : Hub
{
    private readonly ChatService _chat;

    public CourseChatHub(ChatService chat)
    {
        _chat = chat;
    }

    public async Task JoinCourse(int courseId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
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
        if (userId is null)
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

    private static string GroupName(int courseId)
        => $"course-{courseId}";
}
