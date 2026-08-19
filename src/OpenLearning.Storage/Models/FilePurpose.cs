namespace OpenLearning.Storage.Models;

/// <summary>What a stored file is used for; drives limits, extensions, and ACL.</summary>
public enum FilePurpose
{
    Avatar,
    Video,
    Courseware,
    Assignment,
    Answer,
    AsyncIO,
}

/// <summary>State of a media asset's rendition pipeline.</summary>
public enum RenditionStatus
{
    Pending,
    Ready,
    Failed,
}
