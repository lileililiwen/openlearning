using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AsyncIO.Services;
using OpenLearning.QuestionIO.Services;

namespace OpenLearning.QuestionIO;

/// <summary>Registers the question import/export module services.</summary>
public static class QuestionIOModuleExtensions
{
    public static IServiceCollection AddQuestionIOModule(this IServiceCollection services)
    {
        services.AddScoped<QuestionImportRateLimiter>();
        services.AddScoped<QuestionImportService>();
        services.AddScoped<QuestionExportService>();
        services.AddScoped<IAsyncIOProcessor>(sp => sp.GetRequiredService<QuestionImportService>());
        services.AddScoped<IAsyncIOProcessor>(sp => sp.GetRequiredService<QuestionExportService>());
        return services;
    }
}
