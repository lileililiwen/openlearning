using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenLearning.Notifications.Models;
using PushSubscriptionEntity = OpenLearning.Notifications.Models.PushSubscription;
using VapidWebPushSubscription = WebPush.PushSubscription;

namespace OpenLearning.Notifications.Channels;

/// <summary>
/// VAPID-backed web-push sender. Sends the rendered notification to every
/// stored subscription for the user and prunes endpoints the push service
/// reports as expired (404/410). Failures are logged and never propagate.
/// </summary>
public sealed class WebPushSender : IWebPushSender
{
    private static readonly Action<ILogger, string, int, Exception?> _logPushFailed =
        LoggerMessage.Define<string, int>(
            LogLevel.Warning,
            new EventId(1, "PushSendFailed"),
            "Web push failed for {UserId} ({StatusCode})");

    private static readonly Action<ILogger, string, Exception?> _logPushUnexpected =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, "PushSendUnexpected"),
            "Web push failed for {UserId}");

    private readonly DbContext _db;
    private readonly string _subject;
    private readonly string _publicKey;
    private readonly string _privateKey;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(
        DbContext db,
        string subject,
        string publicKey,
        string privateKey,
        ILogger<WebPushSender> logger)
    {
        _db = db;
        _subject = subject;
        _publicKey = publicKey;
        _privateKey = privateKey;
        _logger = logger;
    }

    public async Task SendAsync(string userId, string title, string body, string? link)
    {
        var subscriptions = await _db.Set<PushSubscriptionEntity>()
            .Where(s => s.UserId == userId)
            .ToListAsync();
        if (subscriptions.Count == 0)
        {
            return;
        }

        var client = new WebPush.WebPushClient();
        var vapid = new WebPush.VapidDetails(_subject, _publicKey, _privateKey);
        var payload = JsonSerializer.Serialize(new { title, body, link });
        var pruned = false;

        foreach (var subscription in subscriptions)
        {
            var webPushSubscription = new VapidWebPushSubscription(
                subscription.Endpoint,
                subscription.P256Dh,
                subscription.Auth);
            try
            {
                await client.SendNotificationAsync(webPushSubscription, payload, vapid);
            }
            catch (WebPush.WebPushException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                    ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    _db.Set<PushSubscriptionEntity>().Remove(subscription);
                    pruned = true;
                }
                else
                {
                    _logPushFailed(_logger, userId, (int)ex.StatusCode, ex);
                }
            }
            catch (Exception ex)
            {
                _logPushUnexpected(_logger, userId, ex);
            }
        }

        if (pruned)
        {
            await _db.SaveChangesAsync();
        }
    }
}
