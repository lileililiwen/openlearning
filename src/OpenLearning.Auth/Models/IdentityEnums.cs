namespace OpenLearning.Auth.Models;

/// <summary>The identity document type submitted for real-name verification.</summary>
public enum IdType
{
    NationalId = 0,
    Passport = 1,
    Other = 2,
}

/// <summary>Lifecycle of a user's real-name verification request.</summary>
public enum IdentityStatus
{
    Unverified = 0,
    Pending = 1,
    Verified = 2,
    Rejected = 3,
}
