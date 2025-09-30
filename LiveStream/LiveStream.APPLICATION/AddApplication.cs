using LiveStream.APPLICATION.Interfaces;
using LiveStream.APPLICATION.Service;
using Microsoft.Extensions.DependencyInjection;

namespace LiveStream.APPLICATION;

public static class AddApplication
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IStreamManager, StreamManager>();
        services.AddScoped<IJanusService, JanusService>();
        return services;
    }
}
