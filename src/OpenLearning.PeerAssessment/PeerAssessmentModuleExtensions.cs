using Microsoft.Extensions.DependencyInjection;
using OpenLearning.PeerAssessment.Services;

namespace OpenLearning.PeerAssessment;

public static class PeerAssessmentModuleExtensions
{
    public static IServiceCollection AddPeerAssessmentModule(this IServiceCollection services)
    {
        services.AddScoped<PeerReviewService>();
        return services;
    }
}
