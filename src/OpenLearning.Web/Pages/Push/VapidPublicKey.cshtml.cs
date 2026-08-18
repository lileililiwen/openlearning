using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenLearning.Notifications.Configuration;

namespace OpenLearning.Web.Pages.Push;

/// <summary>Serves the VAPID public key for the push registration client.</summary>
public class VapidPublicKeyModel : PageModel
{
    private readonly ChannelOptions _channels;

    public VapidPublicKeyModel(IOptions<ChannelOptions> channels)
    {
        _channels = channels.Value;
    }

    public IActionResult OnGet()
    {
        if (!_channels.PushEnabled || string.IsNullOrWhiteSpace(_channels.VapidPublicKey))
        {
            return new JsonResult(new { enabled = false });
        }

        return new JsonResult(new { enabled = true, publicKey = _channels.VapidPublicKey });
    }
}
