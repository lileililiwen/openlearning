using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace OpenLearning.Navigation.Services;

/// <summary>
/// Reads/writes the signed, HttpOnly <c>nav.collapsed</c> cookie that persists
/// which sidebar groups the user has collapsed (per-browser).
/// </summary>
public class NavPreferencesService
{
    private const string _cookieName = "nav.collapsed";

    private readonly IDataProtectionProvider _dataProtection;
    private readonly IHttpContextAccessor _http;

    public NavPreferencesService(IDataProtectionProvider dataProtection, IHttpContextAccessor http)
    {
        _dataProtection = dataProtection;
        _http = http;
    }

    private IDataProtector _protector => _dataProtection.CreateProtector("OpenLearning.NavPrefs.v1");

    public ISet<string> GetCollapsedGroups()
    {
        var value = _http.HttpContext?.Request.Cookies[_cookieName];
        if (string.IsNullOrEmpty(value))
        {
            return new HashSet<string>();
        }

        try
        {
            var plain = _protector.Unprotect(value);
            return JsonSerializer.Deserialize<HashSet<string>>(plain) ?? new HashSet<string>();
        }
        catch (Exception)
        {
            return new HashSet<string>();
        }
    }

    public void ToggleCollapsed(string groupKey)
    {
        var collapsed = GetCollapsedGroups();
        if (!collapsed.Add(groupKey))
        {
            collapsed.Remove(groupKey);
        }

        var json = JsonSerializer.Serialize(collapsed);
        var protectedValue = _protector.Protect(json);
        var response = _http.HttpContext?.Response;
        if (response is not null)
        {
            // S2092: the app runs over plain HTTP in local development and its
            // other cookies (auth, antiforgery) are not Secure; a Secure flag
            // here would make the collapse state silently stop persisting.
#pragma warning disable S2092
            response.Cookies.Append(_cookieName, protectedValue, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365),
            });
#pragma warning restore S2092
        }
    }
}
