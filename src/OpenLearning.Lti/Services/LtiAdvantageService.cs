using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assignments.Models;
using OpenLearning.Enrollment.Models;
using OpenLearning.Lti.Models;

namespace OpenLearning.Lti.Services;

using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

public static class LtiScopes
{
    public const string NrpsReadonly = "https://purl.imsglobal.org/spec/lti-nrps/scope/contextmembership.readonly";
    public const string AgsLineItem = "https://purl.imsglobal.org/spec/lti-ags/scope/lineitem";
    public const string AgsScore = "https://purl.imsglobal.org/spec/lti-ags/scope/score";
}

public sealed class LtiAdvantageService
{
    private static readonly string[] _learnerRoles = ["Learner"];
    private readonly DbContext _db;
    public LtiAdvantageService(DbContext db)
    {
        _db = db;
    }

    public async Task<string> CreateDeepLinkResponseAsync(int registrationId, int contextMappingId, string data, IEnumerable<(string Title, string Url)> links)
    {
        var registration = await RequireCapability(registrationId, LtiCapabilities.DeepLinking);
        var mapping = await _db.Set<LtiContextMapping>().SingleAsync(x => x.Id == contextMappingId && x.Deployment.RegistrationId == registrationId && x.Deployment.IsEnabled);
        var items = new List<object>();
        foreach (var link in links)
        {
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Deep-link targets must use HTTPS.");
            var entity = new LtiResourceLink { ContextMappingId = mapping.Id, ResourceLinkId = Guid.NewGuid().ToString("N"), Title = link.Title.Trim(), TargetUrl = link.Url };
            _db.Add(entity);
            items.Add(new { type = "ltiResourceLink", title = entity.Title, url = entity.TargetUrl, custom = new { resourceLinkId = entity.ResourceLinkId } });
        }
        await _db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { iss = registration.ClientId, aud = registration.Issuer, data, content_items = items });
    }

    public async Task<IReadOnlyList<object>> GetRosterAsync(int registrationId, int contextMappingId, ISet<string> scopes)
    {
        await RequireScopeAndCapability(registrationId, LtiCapabilities.Nrps, scopes, LtiScopes.NrpsReadonly);
        var mapping = await _db.Set<LtiContextMapping>().AsNoTracking().SingleAsync(x => x.Id == contextMappingId && x.Deployment.RegistrationId == registrationId && x.Deployment.IsEnabled);
        return await _db.Set<EnrollmentEntity>().AsNoTracking().Where(x => x.CourseId == mapping.CourseId)
            .Select(x => (object)new { user_id = x.StudentId, roles = _learnerRoles, status = x.RevokedAt == null ? "Active" : "Inactive" }).ToListAsync();
    }

    public async Task<(bool Applied, string? Error)> PutScoreAsync(int registrationId, int lineItemId, string operationId, string subject, decimal score, ISet<string> scopes)
    {
        await RequireScopeAndCapability(registrationId, LtiCapabilities.Ags, scopes, LtiScopes.AgsScore);
        if (string.IsNullOrWhiteSpace(operationId) || operationId.Length > 200)
            return (false, "A valid operation ID is required.");
        var lineItem = await _db.Set<LtiLineItem>().Include(x => x.ContextMapping).ThenInclude(x => x.Deployment)
            .SingleOrDefaultAsync(x => x.Id == lineItemId && x.ContextMapping.Deployment.RegistrationId == registrationId && x.ContextMapping.Deployment.IsEnabled);
        if (lineItem is null || score < 0 || score > lineItem.MaximumScore)
            return (false, "Unknown line item or score outside its bounds.");
        var prior = await _db.Set<LtiScoreOperation>().AsNoTracking().SingleOrDefaultAsync(x => x.LineItemId == lineItemId && x.OperationId == operationId);
        if (prior is not null)
        { await Audit(registrationId, "ags.score.duplicate", true, operationId); return (true, null); }
        if (lineItem.AssignmentId is int assignmentId)
        {
            var identity = await _db.Set<LtiSubject>().AsNoTracking().SingleOrDefaultAsync(x => x.DeploymentId == lineItem.ContextMapping.DeploymentId && x.Subject == subject);
            if (identity?.UserId is null)
                return (false, "LTI subject is not linked to a local user.");
            var submission = await _db.Set<AssignmentSubmission>().SingleOrDefaultAsync(x => x.AssignmentId == assignmentId && x.StudentId == identity.UserId);
            if (submission is null)
                return (false, "No bounded assignment submission exists.");
            submission.Score = (int)Math.Round(score / lineItem.MaximumScore * 100, MidpointRounding.AwayFromZero);
            submission.GradedAt = DateTime.UtcNow;
            submission.GradedBy = "lti:" + registrationId;
        }
        _db.Add(new LtiScoreOperation { LineItemId = lineItemId, OperationId = operationId, Subject = subject, Score = score, CreatedAt = DateTime.UtcNow });
        await Audit(registrationId, "ags.score.applied", true, operationId);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<LtiLineItem> CreateLineItemAsync(int registrationId, int contextMappingId, string externalId, int? assignmentId, decimal maximumScore, ISet<string> scopes)
    {
        await RequireScopeAndCapability(registrationId, LtiCapabilities.Ags, scopes, LtiScopes.AgsLineItem);
        if (maximumScore <= 0 || string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("Line item ID and positive maximum score are required.");
        var allowed = await _db.Set<LtiContextMapping>().AnyAsync(x => x.Id == contextMappingId && x.Deployment.RegistrationId == registrationId && x.Deployment.IsEnabled);
        if (!allowed)
            throw new InvalidOperationException("Context is outside this registration.");
        var item = new LtiLineItem { ContextMappingId = contextMappingId, ExternalLineItemId = externalId.Trim(), AssignmentId = assignmentId, MaximumScore = maximumScore };
        _db.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private async Task<LtiRegistration> RequireCapability(int id, LtiCapabilities capability)
    {
        return await _db.Set<LtiRegistration>().SingleAsync(x => x.Id == id && x.IsEnabled && x.RevokedAt == null && (x.Capabilities & capability) == capability);
    }

    private async Task RequireScopeAndCapability(int id, LtiCapabilities capability, ISet<string> scopes, string required) { if (!scopes.Contains(required)) { await Audit(id, "scope.denied", false, required); await _db.SaveChangesAsync(); throw new UnauthorizedAccessException("Required OAuth scope is missing."); } _ = await RequireCapability(id, capability); }
    private Task Audit(int registrationId, string type, bool success, string detail) { _db.Add(new LtiAuditEvent { RegistrationId = registrationId, EventType = type, Succeeded = success, Detail = detail, CreatedAt = DateTime.UtcNow }); return Task.CompletedTask; }
}

public sealed class LtiAdminService
{
    private readonly DbContext _db;
    public LtiAdminService(DbContext db)
    {
        _db = db;
    }

    public Task<List<LtiRegistration>> ListAsync()
    {
        return _db.Set<LtiRegistration>().AsNoTracking().Include(x => x.Deployments).ThenInclude(x => x.ContextMappings).OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<LtiRegistration> CreateAsync(string name, string issuer, string clientId, string authorizationEndpoint, string jwksUrl, string? tokenEndpoint, LtiCapabilities capabilities)
    {
        ValidateHttps(issuer, authorizationEndpoint, jwksUrl, tokenEndpoint);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Name and client ID are required.");
        var item = new LtiRegistration { Name = name.Trim(), Issuer = issuer.TrimEnd('/'), ClientId = clientId.Trim(), AuthorizationEndpoint = authorizationEndpoint, JwksUrl = jwksUrl, TokenEndpoint = string.IsNullOrWhiteSpace(tokenEndpoint) ? null : tokenEndpoint, Capabilities = capabilities, CreatedAt = DateTime.UtcNow };
        _db.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }
    public async Task<LtiDeployment> AddDeploymentAsync(int registrationId, string deploymentId) { var registration = await _db.Set<LtiRegistration>().SingleAsync(x => x.Id == registrationId && x.RevokedAt == null); var item = new LtiDeployment { RegistrationId = registration.Id, DeploymentId = deploymentId.Trim() }; _db.Add(item); await _db.SaveChangesAsync(); return item; }
    public async Task MapContextAsync(int deploymentId, string contextId, int courseId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
        {
            throw new ArgumentException("Context ID is required.");
        }
        _db.Add(new LtiContextMapping { DeploymentId = deploymentId, ExternalContextId = contextId.Trim(), CourseId = courseId });
        await _db.SaveChangesAsync();
    }
    public async Task RevokeAsync(int id) { var item = await _db.Set<LtiRegistration>().SingleAsync(x => x.Id == id); item.IsEnabled = false; item.RevokedAt = DateTime.UtcNow; _db.Add(new LtiAuditEvent { RegistrationId = id, EventType = "registration.revoked", Succeeded = true, Detail = "Registration revoked by administrator.", CreatedAt = DateTime.UtcNow }); await _db.SaveChangesAsync(); }
    public async Task<LtiSigningKey> RotateKeyAsync()
    {
        using var rsa = RSA.Create(3072);
        var key = new LtiSigningKey { KeyId = Guid.NewGuid().ToString("N"), PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(), PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(), CreatedAt = DateTime.UtcNow };
        var active = await _db.Set<LtiSigningKey>().Where(x => x.RetiredAt == null).ToListAsync();
        foreach (var old in active)
        {
            old.RetiredAt = DateTime.UtcNow;
        }
        _db.Add(key);
        await _db.SaveChangesAsync();
        return key;
    }
    public Task<List<LtiAuditEvent>> AuditAsync(int? registrationId = null)
    {
        return _db.Set<LtiAuditEvent>().AsNoTracking().Where(x => registrationId == null || x.RegistrationId == registrationId).OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync();
    }

    private static void ValidateHttps(params string?[] urls) { foreach (var value in urls.Where(x => !string.IsNullOrWhiteSpace(x))) { if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) { throw new ArgumentException("All LTI endpoints must use HTTPS."); } } }
}
