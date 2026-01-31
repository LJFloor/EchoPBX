namespace EchoPBX.Repositories.TrunkWrite;

public interface ITrunkWriteRepository
{
    /// <summary>
    /// Creates a new trunk.
    /// </summary>
    /// <param name="trunk">The trunk to create.</param>
    Task<int> Create(Models.Trunk trunk);
    
    /// <summary>
    /// Updates an existing trunk.
    /// </summary>
    /// <param name="trunk">The trunk to update.</param>
    Task Update(Models.Trunk trunk);
    
    /// <summary>
    /// Deletes a trunk by ID.
    /// </summary>
    /// <param name="id">The trunk ID.</param>
    Task Delete(int id);
}