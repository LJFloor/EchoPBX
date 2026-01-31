namespace EchoPBX.Repositories.QueueWrite;

public interface IQueueWriteRepository
{
    /// <summary>
    /// Updates an existing queue.
    /// </summary>
    /// <param name="queue">The queue to update</param>
    Task Update(Models.Queue queue);

    /// <summary>
    /// Create a new queue.
    /// </summary>
    /// <param name="queue">The queue to create</param>
    Task Create(Models.Queue queue);

    /// <summary>
    /// Delete an existing queue.
    /// </summary>
    /// <param name="id">The identifier of the queue to delete</param>
    Task Delete(int id);
}