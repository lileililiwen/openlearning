using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using OpenLearning.Lti.Configuration;
using OpenLearning.Lti.Models;
using OpenLearning.Lti.Services;
using Xunit;

namespace OpenLearning.UnitTests;

public sealed class LtiProtocolTests
{
    [Fact]
    public async Task Valid_launch_is_accepted_once_and_roles_are_context_scoped()
    {
        using var rsa = RSA.Create(2048);
        await using var db = CreateDb();
        var registration = SeedRegistration(db);
        var protocol = CreateProtocol(db, rsa, "key-1");
        var login = await protocol.BeginLoginAsync(registration.Id, "hint", "https://tool.example/Lti/Launch");
        var query = QueryHelpers.ParseQuery(login.Query);
        var token = Sign(rsa, "key-1", Payload(registration, query["nonce"].ToString(), "deployment", "course-ctx", "subject-1", "Instructor"));

        var result = await protocol.ValidateLaunchAsync(query["state"].ToString(), token);
        var replay = await protocol.ValidateLaunchAsync(query["state"].ToString(), token);

        Assert.True(result.Ok);
        Assert.Equal(42, result.CourseId);
        Assert.True(result.IsInstructor);
        Assert.False(replay.Ok);
        Assert.Contains("state", replay.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await db.Set<LtiSubject>().ToListAsync());
    }

    [Theory]
    [InlineData("wrong-audience", "deployment", "course-ctx")]
    [InlineData("client-1", "unknown", "course-ctx")]
    [InlineData("client-1", "deployment", "unknown-context")]
    public async Task Invalid_audience_deployment_or_context_fails_closed(string audience, string deployment, string context)
    {
        using var rsa = RSA.Create(2048);
        await using var db = CreateDb();
        var registration = SeedRegistration(db);
        var protocol = CreateProtocol(db, rsa, "key-1");
        var login = await protocol.BeginLoginAsync(registration.Id, "hint", "https://tool.example/Lti/Launch");
        var query = QueryHelpers.ParseQuery(login.Query);
        var payload = Payload(registration, query["nonce"].ToString(), deployment, context, "subject-1", "Learner");
        payload["aud"] = audience;

        var result = await protocol.ValidateLaunchAsync(query["state"].ToString(), Sign(rsa, "key-1", payload));

        Assert.False(result.Ok);
        Assert.Empty(await db.Set<LtiSubject>().ToListAsync());
    }

    [Fact]
    public async Task Unknown_signing_key_refreshes_jwks_and_still_fails_closed()
    {
        using var trusted = RSA.Create(2048);
        using var untrusted = RSA.Create(2048);
        await using var db = CreateDb();
        var registration = SeedRegistration(db);
        var protocol = CreateProtocol(db, trusted, "trusted");
        var login = await protocol.BeginLoginAsync(registration.Id, "hint", "https://tool.example/Lti/Launch");
        var query = QueryHelpers.ParseQuery(login.Query);

        var result = await protocol.ValidateLaunchAsync(query["state"].ToString(), Sign(untrusted, "unknown", Payload(registration, query["nonce"].ToString(), "deployment", "course-ctx", "subject-1", "Learner")));

        Assert.False(result.Ok);
        Assert.Empty(await db.Set<LtiSubject>().ToListAsync());
    }

    [Fact]
    public async Task Ags_requires_exact_scope_and_duplicate_operation_is_idempotent()
    {
        await using var db = CreateDb();
        var registration = SeedRegistration(db);
        var mapping = await db.Set<LtiContextMapping>().SingleAsync();
        var service = new LtiAdvantageService(db);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateLineItemAsync(registration.Id, mapping.Id, "line-1", null, 100, new HashSet<string>()));
        var item = await service.CreateLineItemAsync(registration.Id, mapping.Id, "line-1", null, 100, new HashSet<string> { LtiScopes.AgsLineItem });
        var scopes = new HashSet<string> { LtiScopes.AgsScore };
        Assert.True((await service.PutScoreAsync(registration.Id, item.Id, "operation-1", "subject", 75, scopes)).Applied);
        Assert.True((await service.PutScoreAsync(registration.Id, item.Id, "operation-1", "subject", 75, scopes)).Applied);
        Assert.Single(await db.Set<LtiScoreOperation>().ToListAsync());
    }

    private static LtiTestDb CreateDb()
    {
        var options = new DbContextOptionsBuilder<LtiTestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new LtiTestDb(options);
    }

    private static LtiRegistration SeedRegistration(LtiTestDb db)
    {
        var registration = new LtiRegistration { Name = "Platform", Issuer = "https://platform.example", ClientId = "client-1", AuthorizationEndpoint = "https://platform.example/authorize", JwksUrl = "https://platform.example/jwks", Capabilities = LtiCapabilities.DeepLinking | LtiCapabilities.Nrps | LtiCapabilities.Ags, CreatedAt = DateTime.UtcNow };
        var deployment = new LtiDeployment { Registration = registration, DeploymentId = "deployment" };
        deployment.ContextMappings.Add(new LtiContextMapping { ExternalContextId = "course-ctx", CourseId = 42 });
        registration.Deployments.Add(deployment);
        db.Add(registration);
        db.SaveChanges();
        return registration;
    }

    private static LtiProtocolService CreateProtocol(LtiTestDb db, RSA rsa, string kid)
    {
        var parameters = rsa.ExportParameters(false);
        var jwks = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", kid, alg = "RS256", n = WebEncoders.Base64UrlEncode(parameters.Modulus!), e = WebEncoders.Base64UrlEncode(parameters.Exponent!) } } });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new StaticHandler(jwks)));
        return new LtiProtocolService(db, factory.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    private static Dictionary<string, object> Payload(LtiRegistration registration, string nonce, string deployment, string context, string subject, string role)
    {
        return new()
        {
            ["iss"] = registration.Issuer,
            ["aud"] = registration.ClientId,
            ["sub"] = subject,
            ["nonce"] = nonce,
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
            ["https://purl.imsglobal.org/spec/lti/claim/deployment_id"] = deployment,
            ["https://purl.imsglobal.org/spec/lti/claim/version"] = "1.3.0",
            ["https://purl.imsglobal.org/spec/lti/claim/message_type"] = "LtiResourceLinkRequest",
            ["https://purl.imsglobal.org/spec/lti/claim/context"] = new { id = context },
            ["https://purl.imsglobal.org/spec/lti/claim/roles"] = new[] { "http://purl.imsglobal.org/vocab/lis/v2/membership#" + role }
        };
    }

    private static string Sign(RSA rsa, string kid, object payload)
    {
        var header = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid, typ = "JWT" }));
        var body = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(header + "." + body), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return header + "." + body + "." + WebEncoders.Base64UrlEncode(signature);
    }

    private sealed class StaticHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private sealed class LtiTestDb(DbContextOptions<LtiTestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LtiRegistrationConfiguration).Assembly);
        }
    }
}
