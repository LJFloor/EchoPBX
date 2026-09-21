using EchoPBX.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EchoPBX.Web.Authentication;
using EchoPBX.Data.Services.Settings;

namespace EchoPBX.Web.Controllers;

public record SetupRequest(string AdminUsername, string AdminPassword);

[ApiController, Route("/api/system")]
public class SystemController(EchoDbContext dbContext, ISettingsService settingsService) : ControllerBase
{
    /// <summary>
    /// Checks whether the system has been set up (i.e., if any admin users exist).
    /// </summary>
    [HttpGet("is-setup")]
    public async Task<IActionResult> IsSetup()
    {
        var isSetup = await dbContext.Admins.AnyAsync();
        return Ok(isSetup);
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request)
    {
        var isSetup = await dbContext.Admins.AnyAsync();
        if (isSetup)
        {
            return BadRequest("System is already set up.");
        }

        var admin = new EchoPBX.Data.Models.Admin
        {
            Username = request.AdminUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword)
        };

        dbContext.Admins.Add(admin);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// List the sounds that ship with Asterisk, for use in call flows.
    /// </summary>
    /// <remarks>
    /// Only the top level of the language directory is listed. The subdirectories (digits,
    /// letters, phonetic, dictate) hold fragments Asterisk stitches together itself, not
    /// prompts worth picking from a list.
    /// </remarks>
    [HttpGet("builtin-sounds"), RequireAdmin]
    public IActionResult BuiltinSounds()
    {
        var language = settingsService.Get("AsteriskLanguage");
        if (string.IsNullOrWhiteSpace(language)) language = "en";

        string[] roots = ["/usr/share/asterisk/sounds", "/var/lib/asterisk/sounds"];
        string[] soundExtensions = [".gsm", ".wav", ".ulaw", ".alaw", ".g722", ".sln", ".sln16"];

        var directory = roots
            .SelectMany(root => new[] { Path.Combine(root, language), root })
            .FirstOrDefault(Directory.Exists);

        if (directory == null)
        {
            return Ok(Array.Empty<string>());
        }

        // The same prompt ships as several encodings, so collapse them to the bare name that
        // Playback() expects.
        var sounds = Directory
            .EnumerateFiles(directory)
            .Where(x => soundExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(sounds);
    }
}
