using Microsoft.Extensions.DependencyInjection;
using OpenLearning.StudyTools.Services;

namespace OpenLearning.StudyTools;

public static class StudyToolsModuleExtensions
{
    public static IServiceCollection AddStudyToolsModule(this IServiceCollection services)
    {
        services.AddScoped<StudyToolService>();
        services.AddScoped<LearnerNoteService>();
        return services;
    }
}
