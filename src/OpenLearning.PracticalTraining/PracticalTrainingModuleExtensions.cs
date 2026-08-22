using Microsoft.Extensions.DependencyInjection;
using OpenLearning.PracticalTraining.Services;

namespace OpenLearning.PracticalTraining;

public static class PracticalTrainingModuleExtensions
{
    public static IServiceCollection AddPracticalTrainingModule(this IServiceCollection services)
    {
        services.AddScoped<PracticalTrainingService>();
        return services;
    }
}
