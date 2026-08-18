namespace OpenLearning.Storage.Services;

/// <summary>
/// Local-disk blob provider. The root is configured at startup; keys are
/// server-generated, so a path-traversal guard is defense in depth.
/// </summary>
public sealed class LocalStorageProvider : IStorageProvider
{
    private readonly string _root;

    public LocalStorageProvider(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Storage key is required.", nameof(key));
        }

        if (key.Contains("..", StringComparison.Ordinal) || key.StartsWith('/') || key.StartsWith('\\'))
        {
            throw new ArgumentException("Storage key is not safe.", nameof(key));
        }

        return Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
    }

    public async Task SaveAsync(Stream stream, string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var destination = File.Create(path);
        await stream.CopyToAsync(destination, cancellationToken);
    }

    public Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
