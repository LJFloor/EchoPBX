using System.Collections.Concurrent;
using EchoPBX.Data;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Models;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Web.Controllers;

public record AdminLoginRequestBody(string Username, string Password);

public record WebPhoneLoginRequestBody(int ExtensionNumber, string Password);

[ApiController, Route("/api/auth")]
public class AuthenticationController(EchoDbContext dbContext, ILogger<AuthenticationController> logger) : ControllerBase
{
    private const int MaxLoginAttempts = 5;
    private const string BlockedMessage = "Too many failed login attempts. Please try again later.";
    private static readonly TimeSpan BlacklistDuration = TimeSpan.FromMinutes(15);
    private static readonly ConcurrentDictionary<string, int> LoginAttempts = new();
    private static readonly ConcurrentDictionary<string, DateTime> BlacklistedIPs = new();

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequestBody body)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress!.ToString();

        var isBlacklisted = BlacklistedIPs.TryGetValue(ipAddress, out var blacklistTime) && DateTime.UtcNow - blacklistTime < BlacklistDuration;
        if (isBlacklisted)
        {
            return new ObjectResult(BlockedMessage) { StatusCode = 429 };
        }

        var admin = await dbContext.Admins.AsNoTracking().Where(x => x.Username == body.Username).FirstOrDefaultAsync();
        if (admin is null || !admin.VerifyPassword(body.Password))
        {
            var attempts = LoginAttempts.AddOrUpdate(ipAddress, 1, (_, count) => count + 1);
            if (attempts >= MaxLoginAttempts)
            {
                BlacklistedIPs[ipAddress] = DateTime.UtcNow;
                LoginAttempts.TryRemove(ipAddress, out _);
                logger.LogWarning(
                    "IP address {IPAddress} has been blacklisted for {Duration} due to too many failed login attempts (more than {MaxLoginAttempts}). Please note that restarting the server will reset this counter",
                    ipAddress, BlacklistDuration, MaxLoginAttempts);
                return new ObjectResult(BlockedMessage) { StatusCode = 429 };
            }

            return Unauthorized("Invalid username or password");
        }

        var token = StringHelper.GenerateRandomString(128);
        dbContext.Add(new AccessToken
        {
            AdminId = admin.Id,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
        });

        await dbContext.SaveChangesAsync();

        HttpContext.Response.Cookies.Append(AuthenticationMiddleware.TokenCookieName, token);
        return Ok("Login successful");
    }

    [HttpPost("webphone/login")]
    public async Task<IActionResult> WebPhoneLogin([FromBody] WebPhoneLoginRequestBody body)
    {
        var extension = await dbContext.Extensions
            .Where(x => x.ExtensionNumber == body.ExtensionNumber && x.Password == body.Password)
            .Select(x => new { x.DisplayName })
            .FirstOrDefaultAsync();

        if (extension is null)
        {
            return Unauthorized("Invalid extension number or password");
        }
        
        return Ok(extension);
    }
}