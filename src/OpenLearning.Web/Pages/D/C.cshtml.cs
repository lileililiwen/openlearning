using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.D;

/// <summary>Public affiliate redirect: records a click and forwards to the course.</summary>
public class CModel : PageModel
{
    private const string _cookieName = "ol_aff";

    private readonly DistributionService _distribution;

    public CModel(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var link = await _distribution.GetLinkBySlugAsync(slug);
        if (link is null)
        {
            return NotFound();
        }

        var anonymousId = Request.Cookies[_cookieName];
        if (string.IsNullOrEmpty(anonymousId))
        {
            anonymousId = Guid.NewGuid().ToString("N");
#pragma warning disable S2092 // Secure over HTTPS; local dev serves HTTP so the flag is conditional.
            Response.Cookies.Append(_cookieName, anonymousId, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
            });
#pragma warning restore S2092
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _distribution.RecordClickAsync(
            link.Id, anonymousId, Hash(ip), Request.Headers.UserAgent.ToString());

        // Never cache the redirect so clicks are always fresh.
        Response.Headers.CacheControl = "no-store";
        return Redirect($"/Courses/Details?id={link.CourseId}");
    }

    private static string? Hash(string? value)
    {
        return value is null
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
