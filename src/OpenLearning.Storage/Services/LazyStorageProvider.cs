using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Storage.Services;

/// <summary>
/// Delegating provider that resolves the real backend once, on first use. The
/// strategy is read from system-config (or appsettings) at that point — after
/// the app has migrated — so an admin's saved strategy is applied on the next
/// application start without a hot data-plane swap.
/// </summary>
public sealed class LazyStorageProvider : IStorageProvider, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly string _localRoot;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IStorageProvider? _inner;

    public LazyStorageProvider(IServiceProvider services, string localRoot)
    {
        _services = services;
        _localRoot = localRoot;
    }

    private async Task<IStorageProvider> ResolveAsync()
    {
        if (_inner is not null)
        {
            return _inner;
        }

        await _gate.WaitAsync();
        try
        {
            if (_inner is null)
            {
                using var scope = _services.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<SystemConfigService>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                _inner = await StorageProviderFactory.CreateAsync(configuration, config, _localRoot, httpClientFactory);
            }

            return _inner;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(Stream stream, string key, CancellationToken cancellationToken = default)
    {
        await (await ResolveAsync()).SaveAsync(stream, key, cancellationToken);
    }

    public async Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        return await (await ResolveAsync()).OpenAsync(key, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await (await ResolveAsync()).DeleteAsync(key, cancellationToken);
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
