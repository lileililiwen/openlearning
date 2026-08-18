using Microsoft.AspNetCore.Authorization;

namespace OpenLearning.Auth.Authorization;

/// <summary>
/// Fails for accounts an admin has suspended. Added to the role policies so a
/// suspended user is blocked from every role-gated surface (learning, teaching,
/// admin) while still being able to see the public catalog.
/// </summary>
public sealed class NotSuspendedRequirement : IAuthorizationRequirement
{
}
