using Microsoft.AspNetCore.Identity;

namespace OpenLearning.Auth.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When true the account is blocked from learning, teaching, and chat.</summary>
    public bool IsSuspended { get; set; }

    /// <summary>Short public biography shown on the profile and instructor pages.</summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>Avatar image URL (plain URL string; uploads are out of scope).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>The legal name submitted for real-name verification.</summary>
    public string? RealName { get; set; }

    /// <summary>Identity document type submitted for verification.</summary>
    public IdType IdType { get; set; }

    /// <summary>SHA-256 hash of the submitted identity number (never stored plaintext).</summary>
    public string? IdNumberHash { get; set; }

    /// <summary>Verification lifecycle state.</summary>
    public IdentityStatus IdentityStatus { get; set; } = IdentityStatus.Unverified;

    /// <summary>When the identity was approved (null until verified).</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Admin note attached at approval/rejection time.</summary>
    public string? VerificationNote { get; set; }

    /// <summary>Optional document URL submitted as supporting evidence.</summary>
    public string? VerificationDocumentUrl { get; set; }
}
