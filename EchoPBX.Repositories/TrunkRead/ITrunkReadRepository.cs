namespace EchoPBX.Repositories.TrunkRead;

public interface ITrunkReadRepository
{
    /// <summary>
    /// Lists all trunks.
    /// </summary>
    Task<Models.Trunk[]> List();
    
    /// <summary>
    /// Gets a trunk by ID.
    /// </summary>
    /// <param name="id">The trunk ID.</param>
    Task<Models.Trunk?> Get(int id);
}