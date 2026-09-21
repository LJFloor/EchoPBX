namespace EchoPBX.Repositories.CallFlowWrite;

public interface ICallFlowWriteRepository
{
    /// <summary>
    /// Create a new call flow and apply it to Asterisk.
    /// </summary>
    Task Create(Models.CallFlow callFlow);

    /// <summary>
    /// Update an existing call flow and apply it to Asterisk.
    /// </summary>
    Task Update(Models.CallFlow callFlow);

    /// <summary>
    /// Delete a call flow, its uploaded sounds, and apply the change to Asterisk.
    /// </summary>
    /// <param name="id">The ID of the call flow to delete.</param>
    Task Delete(int id);
}
