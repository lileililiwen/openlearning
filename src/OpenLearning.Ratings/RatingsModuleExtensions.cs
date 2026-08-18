using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Ratings.Services;

namespace OpenLearning.Ratings;

public static class RatingsModuleExtensions
{
    public static IServiceCollection AddRatingsModule(this IServiceCollection services)
    {
        services.AddScoped<ReviewService>();
        return services;
    }
}
