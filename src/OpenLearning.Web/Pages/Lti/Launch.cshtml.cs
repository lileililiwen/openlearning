using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Lti.Services;

namespace OpenLearning.Web.Pages.Lti;

#pragma warning disable S4502 // LTI 1.3 response_mode=form_post originates at the registered external platform.
[AllowAnonymous]
[IgnoreAntiforgeryToken]
#pragma warning restore S4502
public sealed class LaunchModel : PageModel
{
    private readonly LtiProtocolService _protocol;
    public LaunchModel(LtiProtocolService protocol)
    {
        _protocol = protocol;
    }

    public LtiLaunchResult Result { get; private set; } = new(false, "Launch data is required.");
    public async Task OnPostAsync(string state, [FromForm(Name = "id_token")] string idToken)
    {
        Result = await _protocol.ValidateLaunchAsync(state, idToken, HttpContext.TraceIdentifier);
    }
}
