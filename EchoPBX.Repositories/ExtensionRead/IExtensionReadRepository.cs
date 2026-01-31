namespace EchoPBX.Repositories.ExtensionRead;

public interface IExtensionReadRepository
{
    /// <summary>
    /// List all extensions
    /// </summary>
    Task<Models.Extension[]> List();
    
    /// <summary>
    /// Get an extension by its extension number
    /// </summary>
    /// <param name="extensionNumber">The extension number to get</param>
    Task<Models.Extension?> Get(int extensionNumber);
}