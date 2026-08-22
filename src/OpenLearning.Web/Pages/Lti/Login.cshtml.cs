using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Lti.Services;

namespace OpenLearning.Web.Pages.Lti;

#pragma warning disable S4502 // LTI OIDC login initiation is an external platform POST and cannot carry our antiforgery token.
[AllowAnonymous]
[IgnoreAntiforgeryToken]
#pragma warning restore S4502
public sealed class LoginModel : PageModel
{
    private readonly LtiProtocolService _protocol;
    public LoginModel(LtiProtocolService protocol)
    {
        _protocol = protocol;
    }

    public async Task<IActionResult> OnGetAsync(int registrationId, string loginHint, string targetLinkUri, string? ltiMessageHint)
    {
        return Redirect((await _protocol.BeginLoginAsync(registrationId, loginHint, targetLinkUri, ltiMessageHint)).ToString());
    }

    public Task<IActionResult> OnPostAsync(int registrationId, string loginHint, string targetLinkUri, string? ltiMessageHint)
    {
        return OnGetAsync(registrationId, loginHint, targetLinkUri, ltiMessageHint);
    }
}
