using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Assessments.Services;

namespace OpenLearning.Assessments;

public static class AssessmentsModuleExtensions
{
    public static IServiceCollection AddAssessmentsModule(this IServiceCollection services)
    {
        services.AddScoped<QuizService>();
        services.AddScoped<QuestionService>();
        services.AddScoped<AttemptService>();
        services.AddScoped<QuestionBankService>();
        return services;
    }
}
