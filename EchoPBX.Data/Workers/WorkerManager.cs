using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoPBX.Data.Workers;

/// <summary>
/// Manage background workers.
/// </summary>
/// <param name="provider"></param>
/// <param name="logger"></param>
public class WorkerManager(IServiceProvider provider, ILogger<WorkerManager> logger)
{
    public static readonly List<Type> WorkerImplementations = [];
    private readonly List<IWorker> _workers = [];

    public void Start()
    {
        foreach (var @interface in WorkerImplementations)
        {
            if (provider.GetRequiredService(@interface) is not IWorker worker) continue;

            _workers.Add(worker);

            _ = Task.Run(async () =>
            {
                try
                {
                    logger.LogInformation("Starting worker {Name}", worker.GetType().Name);
                    await worker.ExecuteAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            });
        }
    }
}

public static class WorkerManagerExtensions
{
    /// <summary>
    /// Add a worker to <see cref="WorkerManager" /> and add as a singleton to the Service Collection
    /// </summary>
    public static IServiceCollection AddWorker<TInterface, TImplementation>(this IServiceCollection services) where TImplementation : class, TInterface where TInterface : class
    {
        services.AddSingleton<TInterface, TImplementation>();
        WorkerManager.WorkerImplementations.Add(typeof(TInterface));
        return services;
    }
}