using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Surveys.Services;

namespace OpenLearning.Surveys;

public static class SurveysModuleExtensions
{
    public static IServiceCollection AddSurveysModule(this IServiceCollection services)
    {
        services.AddScoped<SurveyService>();
        return services;
    }
}
