using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Storage.Services;

namespace OpenLearning.Storage;

public static class StorageModuleExtensions
{
    /// <summary>
    /// Registers the storage module. The active <see cref="IStorageProvider"/>
    /// is resolved lazily on first use from the admin-configured strategy
    /// (system-config), with <paramref name="storageRoot"/> as the local-disk
    /// fallback root.
    /// </summary>
    public static IServiceCollection AddStorageModule(this IServiceCollection services, string storageRoot)
    {
        services.AddSingleton<IStorageProvider>(sp => new LazyStorageProvider(sp, storageRoot));
        services.AddScoped<StorageService>();
        services.AddSingleton<MediaTranscoder>();
        services.AddHostedService(sp => sp.GetRequiredService<MediaTranscoder>());
        return services;
    }
}
