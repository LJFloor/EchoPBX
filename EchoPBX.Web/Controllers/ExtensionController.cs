using EchoPBX.Repositories.ExtensionRead;
using EchoPBX.Repositories.ExtensionWrite;
using EchoPBX.Repositories.ExtensionWrite.Models;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EchoPBX.Web.Controllers;

[ApiController, RequireAdmin, Route("/api/extensions")]
public class ExtensionController(IExtensionReadRepository extensionReadRepository, IExtensionWriteRepository extensionWriteRepository) : ControllerBase
{
    /// <summary>
    /// List all extensions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var extensions = await extensionReadRepository.List();
        return Ok(extensions);
    }

    /// <summary>
    /// Get a single extension by extension number
    /// </summary>
    /// <param name="extensionNumber">The extension number to get</param>
    [HttpGet("{extensionNumber:int}")]
    public async Task<IActionResult> GetExtension(int extensionNumber)
    {
        var extension = await extensionReadRepository.Get(extensionNumber);
        return extension == null ? NotFound() : Ok(extension);
    }

    /// <summary>
    /// Delete an extension by extension number
    /// </summary>
    /// <param name="extensionNumber">The extension number to delete</param>
    [HttpDelete("{extensionNumber:int}")]
    public async Task<IActionResult> DeleteExtension(int extensionNumber)
    {
        await extensionWriteRepository.Delete(extensionNumber);
        return NoContent();
    }

    /// <summary>
    /// Create a new extension
    /// </summary>
    /// <param name="extension">The extension to create</param>
    [HttpPost]
    public async Task<IActionResult> CreateExtension([FromBody] Extension extension)
    {
        await extensionWriteRepository.Create(extension);
        return NoContent();
    }

    /// <summary>
    /// Update an existing extension
    /// </summary>
    /// <param name="extension">The extension to update</param>
    [HttpPut]
    public async Task<IActionResult> UpdateExtension([FromBody] Extension extension)
    {
        await extensionWriteRepository.Update(extension);
        return NoContent();
    }

    /// <summary>
    /// Locate the extension by calling it. This will make all devices associated with the extension ring.
    /// </summary>
    [HttpPost("{extensionNumber:int}/ring")]
    public async Task<IActionResult> Locate(int extensionNumber)
    {
        string[] lines =
        [
            $"Channel: PJSIP/{extensionNumber}",
            "CallerID: SYSTEM <SYSTEM>",
            "Context: system",
            "MaxRetries: 0",
            "WaitTime: 4",
            "Priority: 1",
            "Extension: locate-extension",
        ];

        var tempPath = Path.GetTempFileName() + ".call";
        await System.IO.File.WriteAllLinesAsync(tempPath, lines);
        System.IO.File.Move(tempPath, "/var/spool/asterisk/outgoing/" + Path.GetFileName(tempPath));
        return NoContent();
    }
}