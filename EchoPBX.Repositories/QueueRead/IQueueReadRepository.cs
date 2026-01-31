namespace EchoPBX.Repositories.QueueRead;

public interface IQueueReadRepository
{
    /// <summary>
    /// Lists all queues.
    /// </summary>
    Task<Models.Queue[]> List();
    
    /// <summary>
    /// Gets a queue by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the queue.</param>
    Task<Models.Queue?> GetById(int id);
}