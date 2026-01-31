namespace EchoPBX.Repositories.CdrRead;

public interface ICdrReadRepository
{
    /// <summary>
    /// List all CDR entries, limited to n entries
    /// </summary>
    /// <param name="n">Number of entries to return</param>
    /// <returns>CDR entries</returns>
    Task<Models.CdrEntry[]> List(int n = 100);
}