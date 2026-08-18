using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Push;

[Authorize]
public class SubscribeModel : PageModel
{
    private readonly PushSubscriptionService _subscriptions;

    public SubscribeModel(PushSubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public class SubscriptionInput
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [JsonPropertyName("keys")]
        public KeysInput? Keys { get; set; }
    }

    public class KeysInput
    {
        [JsonPropertyName("p256dh")]
        public string P256Dh { get; set; } = string.Empty;

        [JsonPropertyName("auth")]
        public string Auth { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync([FromBody] SubscriptionInput input)
    {
        if (string.IsNullOrWhiteSpace(input?.Endpoint) ||
            string.IsNullOrWhiteSpace(input.Keys?.P256Dh) ||
            string.IsNullOrWhiteSpace(input.Keys?.Auth))
        {
            return new JsonResult(new { ok = false, error = "Subscription details are incomplete." })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new JsonResult(new { ok = false, error = "Unknown user." })
            {
                StatusCode = StatusCodes.Status401Unauthorized,
            };
        }

        await _subscriptions.SubscribeAsync(userId, input.Endpoint, input.Keys.P256Dh, input.Keys.Auth);
        return new JsonResult(new { ok = true });
    }
}
