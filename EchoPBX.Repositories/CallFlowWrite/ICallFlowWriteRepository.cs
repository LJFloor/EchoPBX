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

    /// <summary>
    /// Moves uploaded sounds a flow uses from outside its own folder into it, so they are kept
    /// and cleaned up like any other upload. Only the MoveDtmfMenusToCallFlows migration leaves
    /// sounds elsewhere: it points each new flow at its trunk's announcement, since a migration
    /// cannot move files. Safe to run at every startup.
    /// </summary>
    Task MoveStraySounds();
}
