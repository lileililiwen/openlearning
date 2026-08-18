using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Chat.Services;

namespace OpenLearning.Chat;

public static class ChatModuleExtensions
{
    public static IServiceCollection AddChatModule(this IServiceCollection services)
    {
        services.AddScoped<ChatService>();
        return services;
    }
}
