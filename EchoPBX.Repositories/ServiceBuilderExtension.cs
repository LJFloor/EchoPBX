using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EchoPBX.Repositories;

public static class ServiceBuilderExtension
{
    /// <summary>
    /// Adds all the EchoPBX repositories to the service collection.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<ExtensionRead.IExtensionReadRepository, ExtensionRead.ExtensionReadRepository>()
            .AddScoped<ExtensionWrite.IExtensionWriteRepository, ExtensionWrite.ExtensionWriteRepository>()
            .AddScoped<TrunkRead.ITrunkReadRepository, TrunkRead.TrunkReadRepository>()
            .AddScoped<TrunkWrite.ITrunkWriteRepository, TrunkWrite.TrunkWriteRepository>()
            .AddScoped<QueueRead.IQueueReadRepository, QueueRead.QueueReadRepository>()
            .AddScoped<QueueWrite.IQueueWriteRepository, QueueWrite.QueueWriteRepository>()
            .AddScoped<CdrRead.ICdrReadRepository, CdrRead.CdrReadRepository>();

        return services;
    }
}