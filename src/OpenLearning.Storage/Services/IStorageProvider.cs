namespace OpenLearning.Storage.Services;

/// <summary>Blob storage abstraction. Keys are server-generated, forward-slash paths.</summary>
public interface IStorageProvider
{
    Task SaveAsync(Stream stream, string key, CancellationToken cancellationToken = default);

    Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
