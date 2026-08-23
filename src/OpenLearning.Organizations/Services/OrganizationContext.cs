using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Organizations.Models;

namespace OpenLearning.Organizations.Services;

public sealed record ActiveOrganization(int Id, string Name, string PrimaryColor, OrganizationRole Role);

public interface IOrganizationContext
{
    Task<ActiveOrganization?> GetActiveAsync();
    Task<bool> SetActiveAsync(int organizationId);
    void Clear();
}

public sealed class OrganizationContext : IOrganizationContext
{
    private const string _cookieName = "ol.organization";
    private readonly DbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IDataProtector _protector;
    private bool _loaded;
    private ActiveOrganization? _active;

    public OrganizationContext(DbContext db, IHttpContextAccessor http, IDataProtectionProvider protection)
    { _db = db; _http = http; _protector = protection.CreateProtector("OpenLearning.ActiveOrganization.v1"); }

    public async Task<ActiveOrganization?> GetActiveAsync()
    {
        if (_loaded)
            return _active;
        _loaded = true;
        var userId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var raw = _http.HttpContext?.Request.Cookies[_cookieName];
        if (userId is null || raw is null)
            return null;
        try
        {
            if (!int.TryParse(_protector.Unprotect(raw), out var id))
                return null;
            _active = await _db.Set<OrganizationMembership>().AsNoTracking()
                .Where(x => x.OrganizationId == id && x.UserId == userId && x.Status == MembershipStatus.Active && x.Organization!.Status == OrganizationStatus.Active)
                .Select(x => new ActiveOrganization(x.OrganizationId, x.Organization!.Name, x.Organization.PrimaryColor, x.Role)).SingleOrDefaultAsync();
        }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
        return _active;
    }

    public async Task<bool> SetActiveAsync(int organizationId)
    {
        var userId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var allowed = userId is not null && await _db.Set<OrganizationMembership>().AnyAsync(x => x.OrganizationId == organizationId && x.UserId == userId && x.Status == MembershipStatus.Active && x.Organization!.Status == OrganizationStatus.Active);
        if (!allowed)
            return false;
#pragma warning disable S2092 // Development supports HTTP; production emits Secure cookies on HTTPS.
        _http.HttpContext!.Response.Cookies.Append(_cookieName, _protector.Protect(organizationId.ToString(System.Globalization.CultureInfo.InvariantCulture)), new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax, Secure = _http.HttpContext.Request.IsHttps });
#pragma warning restore S2092
        _loaded = false;
        _active = null;
        return true;
    }

    public void Clear() { _http.HttpContext?.Response.Cookies.Delete(_cookieName); _loaded = true; _active = null; }
}
