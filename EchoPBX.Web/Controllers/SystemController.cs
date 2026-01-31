using EchoPBX.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Web.Controllers;

public record SetupRequest(string AdminUsername, string AdminPassword);

[ApiController, Route("/api/system")]
public class SystemController(EchoDbContext dbContext) : ControllerBase
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
}