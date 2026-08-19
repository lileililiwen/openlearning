using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AsyncIO.Services;

namespace OpenLearning.AsyncIO;

public static class AsyncIOModuleExtensions
{
    public static IServiceCollection AddAsyncIOModule(this IServiceCollection services)
    {
        services.AddScoped<AsyncIOService>();
        return services;
    }
}
