namespace EchoPBX.Repositories.CallFlowRead;

public interface ICallFlowReadRepository
{
    /// <summary>
    /// List all call flows. The returned definitions are empty; use <see cref="GetBySlug"/> for one flow.
    /// </summary>
    Task<Models.CallFlow[]> List();

    /// <summary>
    /// Get a single call flow, including its full definition.
    /// </summary>
    /// <param name="slug">The slug of the call flow.</param>
    Task<Models.CallFlow?> GetBySlug(string slug);

    /// <summary>
    /// Get a single call flow by its ID, including its full definition.
    /// </summary>
    /// <param name="id">The ID of the call flow.</param>
    Task<Models.CallFlow?> GetById(int id);
}
