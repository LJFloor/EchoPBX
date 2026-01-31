namespace EchoPBX.Data.Workers;

/// <summary>
/// Represents a generic worker that can execute tasks asynchronously in the background.
/// </summary>
public interface  IWorker
{
    /// <summary>
    /// Executes the worker's tasks asynchronously. You can block this method using the provided CancellationToken to stop execution when needed.
    /// </summary>
    public Task ExecuteAsync(CancellationToken stoppingToken);
}