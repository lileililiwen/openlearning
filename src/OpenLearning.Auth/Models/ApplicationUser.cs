using Microsoft.AspNetCore.Identity;

namespace OpenLearning.Auth.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When true the account is blocked from learning, teaching, and chat.</summary>
    public bool IsSuspended { get; set; }
}
