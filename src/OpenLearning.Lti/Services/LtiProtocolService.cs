using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OpenLearning.Lti.Models;

namespace OpenLearning.Lti.Services;

public sealed record LtiLaunchResult(bool Ok, string? Error, int? CourseId = null, string? Subject = null, bool IsInstructor = false);

public sealed class LtiProtocolService
{
    private const string _deploymentClaim = "https://purl.imsglobal.org/spec/lti/claim/deployment_id";
    private const string _versionClaim = "https://purl.imsglobal.org/spec/lti/claim/version";
    private const string _messageTypeClaim = "https://purl.imsglobal.org/spec/lti/claim/message_type";
    private const string _contextClaim = "https://purl.imsglobal.org/spec/lti/claim/context";
    private const string _rolesClaim = "https://purl.imsglobal.org/spec/lti/claim/roles";
    private readonly DbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public LtiProtocolService(DbContext db, IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<Uri> BeginLoginAsync(int registrationId, string loginHint, string targetLinkUri, string? ltiMessageHint = null)
    {
        var registration = await _db.Set<LtiRegistration>().AsNoTracking().SingleAsync(x => x.Id == registrationId && x.IsEnabled && x.RevokedAt == null);
        RequireHttps(registration.Issuer, registration.AuthorizationEndpoint, registration.JwksUrl, targetLinkUri);
        var state = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(32);
        _db.Set<LtiProtocolToken>().AddRange(
            NewToken(registrationId, "state", state, TimeSpan.FromMinutes(10)),
            NewToken(registrationId, "nonce", nonce, TimeSpan.FromMinutes(10)));
        await _db.SaveChangesAsync();
        var query = new Dictionary<string, string?>
        {
            ["scope"] = "openid",
            ["response_type"] = "id_token",
            ["response_mode"] = "form_post",
            ["prompt"] = "none",
            ["client_id"] = registration.ClientId,
            ["redirect_uri"] = targetLinkUri,
            ["login_hint"] = loginHint,
            ["lti_message_hint"] = ltiMessageHint,
            ["state"] = WebEncoders.Base64UrlEncode(state),
            ["nonce"] = WebEncoders.Base64UrlEncode(nonce)
        };
        return new Uri(QueryHelpers.AddQueryString(registration.AuthorizationEndpoint, query));
    }

    public async Task<LtiLaunchResult> ValidateLaunchAsync(string state, string idToken, string? correlationId = null)
    {
        JsonDocument? header = null;
        JsonDocument? payload = null;
        LtiRegistration? registration = null;
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3)
                return new(false, "Malformed launch token.");
            header = JsonDocument.Parse(WebEncoders.Base64UrlDecode(parts[0]));
            payload = JsonDocument.Parse(WebEncoders.Base64UrlDecode(parts[1]));
            var root = payload.RootElement;
            var issuer = RequiredString(root, "iss");
            registration = await _db.Set<LtiRegistration>().Include(x => x.Deployments).ThenInclude(x => x.ContextMappings)
                .SingleOrDefaultAsync(x => x.Issuer == issuer && x.IsEnabled && x.RevokedAt == null);
            if (registration is null)
                return new(false, "Unknown or revoked issuer.");
            RequireHttps(registration.Issuer, registration.AuthorizationEndpoint, registration.JwksUrl);
            if (!AudienceContains(root, registration.ClientId))
                return await Fail(registration.Id, "launch.audience", "Invalid audience.", correlationId);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var issuedAt = RequiredLong(root, "iat");
            var expiresAt = RequiredLong(root, "exp");
            if (issuedAt < now - 300 || issuedAt > now + 60 || expiresAt < now)
                return await Fail(registration.Id, "launch.timestamp", "Expired or invalid launch timestamp.", correlationId);
            if (RequiredString(root, _versionClaim) != "1.3.0")
                return await Fail(registration.Id, "launch.version", "Unsupported LTI version.", correlationId);
            var messageType = RequiredString(root, _messageTypeClaim);
            if (messageType is not ("LtiResourceLinkRequest" or "LtiDeepLinkingRequest"))
                return await Fail(registration.Id, "launch.message", "Unsupported message type.", correlationId);
            var deploymentId = RequiredString(root, _deploymentClaim);
            var deployment = registration.Deployments.SingleOrDefault(x => x.DeploymentId == deploymentId && x.IsEnabled);
            if (deployment is null)
                return await Fail(registration.Id, "launch.deployment", "Unknown or disabled deployment.", correlationId);
            if (!await ConsumeAsync(registration.Id, "state", state))
                return await Fail(registration.Id, "launch.state", "Invalid or consumed state.", correlationId);
            var nonce = RequiredString(root, "nonce");
            if (!await ConsumeAsync(registration.Id, "nonce", nonce))
                return await Fail(registration.Id, "launch.replay", "Invalid or consumed nonce.", correlationId);
            if (!await VerifySignatureAsync(registration, header.RootElement, parts))
                return await Fail(registration.Id, "launch.signature", "Untrusted signature.", correlationId);
            var contextId = root.GetProperty(_contextClaim).GetProperty("id").GetString() ?? string.Empty;
            var mapping = deployment.ContextMappings.SingleOrDefault(x => x.ExternalContextId == contextId);
            if (mapping is null)
                return await Fail(registration.Id, "launch.context", "Context is not mapped.", correlationId);
            var subject = RequiredString(root, "sub");
            var identity = await _db.Set<LtiSubject>().SingleOrDefaultAsync(x => x.DeploymentId == deployment.Id && x.Subject == subject);
            if (identity is null)
            { identity = new() { DeploymentId = deployment.Id, Subject = subject }; _db.Add(identity); }
            identity.LastLaunchAt = DateTime.UtcNow;
            var instructor = root.TryGetProperty(_rolesClaim, out var roles) && roles.EnumerateArray().Any(x => (x.GetString() ?? "").EndsWith("#Instructor", StringComparison.Ordinal));
            await Audit(registration.Id, "launch.accepted", true, $"course={mapping.CourseId}; instructor={instructor}", correlationId);
            await _db.SaveChangesAsync();
            return new(true, null, mapping.CourseId, subject, instructor);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or CryptographicException or HttpRequestException)
        {
            if (registration is not null)
                await Audit(registration.Id, "launch.invalid", false, ex.Message, correlationId);
            return new(false, "Invalid LTI launch.");
        }
        finally { header?.Dispose(); payload?.Dispose(); }
    }

    private async Task<bool> VerifySignatureAsync(LtiRegistration registration, JsonElement header, string[] parts)
    {
        if (RequiredString(header, "alg") != "RS256")
            return false;
        var kid = RequiredString(header, "kid");
        var keys = await GetJwks(registration, false);
        var key = FindKey(keys, kid);
        if (key is null)
        { keys = await GetJwks(registration, true); key = FindKey(keys, kid); }
        if (key is null)
            return false;
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Modulus = WebEncoders.Base64UrlDecode(RequiredString(key.Value, "n")), Exponent = WebEncoders.Base64UrlDecode(RequiredString(key.Value, "e")) });
        return rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), WebEncoders.Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private async Task<JsonElement> GetJwks(LtiRegistration registration, bool refresh)
    {
        var cacheKey = "lti-jwks:" + registration.Id;
        if (!refresh && _cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return JsonDocument.Parse(cached).RootElement.Clone();
        using var response = await _httpClientFactory.CreateClient().GetAsync(registration.JwksUrl);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("keys", out _))
            throw new JsonException("JWKS has no keys.");
        _cache.Set(cacheKey, json, TimeSpan.FromMinutes(15));
        return doc.RootElement.Clone();
    }

    private static JsonElement? FindKey(JsonElement jwks, string kid) { foreach (var key in jwks.GetProperty("keys").EnumerateArray()) { if (key.TryGetProperty("kid", out var value) && value.GetString() == kid) { return key; } } return null; }
    private async Task<bool> ConsumeAsync(int registrationId, string kind, string raw)
    {
        var hash = Hash(raw);
        var now = DateTime.UtcNow;
        var token = await _db.Set<LtiProtocolToken>().SingleOrDefaultAsync(x => x.RegistrationId == registrationId && x.Kind == kind && x.ValueHash == hash && x.ConsumedAt == null && x.ExpiresAt > now);
        if (token is null)
        {
            return false;
        }
        token.ConsumedAt = now;
        await _db.SaveChangesAsync();
        return true;
    }
    private static LtiProtocolToken NewToken(int registrationId, string kind, byte[] bytes, TimeSpan lifetime)
    {
        return new() { RegistrationId = registrationId, Kind = kind, ValueHash = Hash(WebEncoders.Base64UrlEncode(bytes)), ExpiresAt = DateTime.UtcNow.Add(lifetime) };
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private async Task<LtiLaunchResult> Fail(int registrationId, string type, string detail, string? correlationId) { await Audit(registrationId, type, false, detail, correlationId); await _db.SaveChangesAsync(); return new(false, detail); }
    private Task Audit(int? registrationId, string type, bool succeeded, string detail, string? correlationId) { _db.Add(new LtiAuditEvent { RegistrationId = registrationId, EventType = type, Succeeded = succeeded, Detail = detail[..Math.Min(detail.Length, 2000)], CorrelationId = correlationId, CreatedAt = DateTime.UtcNow }); return Task.CompletedTask; }
    private static string RequiredString(JsonElement e, string name)
    {
        return e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()! : throw new JsonException($"Missing {name}.");
    }

    private static long RequiredLong(JsonElement e, string name)
    {
        return e.TryGetProperty(name, out var p) && p.TryGetInt64(out var value) ? value : throw new JsonException($"Missing {name}.");
    }

    private static bool AudienceContains(JsonElement root, string clientId)
    {
        if (!root.TryGetProperty("aud", out var aud))
        {
            return false;
        }
        return aud.ValueKind == JsonValueKind.String ? aud.GetString() == clientId : aud.ValueKind == JsonValueKind.Array && aud.EnumerateArray().Any(x => x.GetString() == clientId);
    }
    private static void RequireHttps(params string?[] urls) { if (urls.Any(x => !Uri.TryCreate(x, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)) { throw new InvalidOperationException("LTI endpoints must use HTTPS."); } }
}
