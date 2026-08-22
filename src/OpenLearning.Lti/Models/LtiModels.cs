namespace OpenLearning.Lti.Models;

[Flags]
public enum LtiCapabilities { None = 0, DeepLinking = 1, Nrps = 2, Ags = 4 }

public class LtiRegistration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string JwksUrl { get; set; } = string.Empty;
    public string? TokenEndpoint { get; set; }
    public LtiCapabilities Capabilities { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public ICollection<LtiDeployment> Deployments { get; set; } = new List<LtiDeployment>();
}

public class LtiDeployment
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public LtiRegistration Registration { get; set; } = null!;
    public string DeploymentId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public ICollection<LtiContextMapping> ContextMappings { get; set; } = new List<LtiContextMapping>();
}

public class LtiContextMapping
{
    public int Id { get; set; }
    public int DeploymentId { get; set; }
    public LtiDeployment Deployment { get; set; } = null!;
    public string ExternalContextId { get; set; } = string.Empty;
    public int CourseId { get; set; }
}

public class LtiSubject
{
    public int Id { get; set; }
    public int DeploymentId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime LastLaunchAt { get; set; }
}

public class LtiResourceLink
{
    public int Id { get; set; }
    public int ContextMappingId { get; set; }
    public string ResourceLinkId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
}

public class LtiLineItem
{
    public int Id { get; set; }
    public int ContextMappingId { get; set; }
    public LtiContextMapping ContextMapping { get; set; } = null!;
    public string ExternalLineItemId { get; set; } = string.Empty;
    public int? AssignmentId { get; set; }
    public decimal MaximumScore { get; set; } = 100;
}

public class LtiProtocolToken
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ValueHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public class LtiSigningKey
{
    public int Id { get; set; }
    public string KeyId { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
}

public class LtiAuditEvent
{
    public long Id { get; set; }
    public int? RegistrationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LtiScoreOperation
{
    public long Id { get; set; }
    public int LineItemId { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public DateTime CreatedAt { get; set; }
}
