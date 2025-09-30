using LiveStream.APPLICATION.Interfaces;
using LiveStream.INFRASTRUCTURE.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LiveStream.INFRASTRUCTURE;

public static class AddInfrastructure
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
