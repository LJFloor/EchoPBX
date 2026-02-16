using Microsoft.Extensions.DependencyInjection;

namespace EchoPBX.Repositories;

public static class ServiceBuilderExtension
{
    /// <summary>
    /// Adds all the EchoPBX repositories to the service collection.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ExtensionRead.IExtensionReadRepository, ExtensionRead.ExtensionReadRepository>();
        services.AddScoped<ExtensionWrite.IExtensionWriteRepository, ExtensionWrite.ExtensionWriteRepository>();
        services.AddScoped<TrunkRead.ITrunkReadRepository, TrunkRead.TrunkReadRepository>();
        services.AddScoped<TrunkWrite.ITrunkWriteRepository, TrunkWrite.TrunkWriteRepository>();
        services.AddScoped<QueueRead.IQueueReadRepository, QueueRead.QueueReadRepository>();
        services.AddScoped<QueueWrite.IQueueWriteRepository, QueueWrite.QueueWriteRepository>();
        services.AddScoped<CdrRead.ICdrReadRepository, CdrRead.CdrReadRepository>();
        
        return services;
    }
}