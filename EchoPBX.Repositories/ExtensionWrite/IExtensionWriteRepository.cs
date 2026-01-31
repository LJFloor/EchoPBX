namespace EchoPBX.Repositories.ExtensionWrite;

public interface IExtensionWriteRepository
{
    /// <summary>
    /// Update an existing extension
    /// </summary>
    /// <param name="extension">The extension to update</param>
    Task Update(Models.Extension extension);
    
    /// <summary>
    /// Delete an existing extension
    /// </summary>
    /// <param name="extensionNumber">The extension number of the extension to delete</param>
    Task Delete(int extensionNumber);
    
    /// <summary>
    /// Create a new extension
    /// </summary>
    /// <param name="extension">The extension to create</param>
    Task Create(Models.Extension extension);
}