using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Storage.Services;

namespace OpenLearning.Storage;

public static class StorageModuleExtensions
{
    public static IServiceCollection AddStorageModule(this IServiceCollection services, string storageRoot)
    {
        services.AddSingleton<IStorageProvider>(_ => new LocalStorageProvider(storageRoot));
        services.AddScoped<StorageService>();
        services.AddSingleton<MediaTranscoder>();
        services.AddHostedService(sp => sp.GetRequiredService<MediaTranscoder>());
        return services;
    }
}
