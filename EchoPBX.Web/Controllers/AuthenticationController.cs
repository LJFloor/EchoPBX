using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
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
        if (IsBlacklisted(ipAddress))
        {
            return new ObjectResult(BlockedMessage) { StatusCode = 429 };
        }

        var admin = await dbContext.Admins.AsNoTracking().Where(x => x.Username == body.Username).FirstOrDefaultAsync();
        if (admin is null || !admin.VerifyPassword(body.Password))
        {
            return FailedLogin(ipAddress, "Invalid username or password");
        }

        LoginAttempts.TryRemove(ipAddress, out _);

        var token = StringHelper.GenerateRandomString(128);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
        dbContext.Add(new AccessToken
        {
            AdminId = admin.Id,
            Token = token,
            ExpiresAt = expiresAt
        });

        await dbContext.SaveChangesAsync();

        HttpContext.Response.Cookies.Append(AuthenticationMiddleware.TokenCookieName, token, new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddHours(24) });
        HttpContext.Response.Cookies.Append("echopbx_username", admin.Username, new CookieOptions { HttpOnly = false, Expires = DateTimeOffset.UtcNow.AddHours(24) });
        return Ok("Login successful");
    }

    /// <summary>
    /// Check an extension's credentials for the webphone. The webphone registers with Asterisk
    /// itself, so no session is created; this only gives a clear error before it tries.
    /// </summary>
    [HttpPost("phone/login")]
    public async Task<IActionResult> PhoneLogin([FromBody] WebPhoneLoginRequestBody body)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress!.ToString();
        if (IsBlacklisted(ipAddress))
        {
            return new ObjectResult(BlockedMessage) { StatusCode = 429 };
        }

        var extension = await dbContext.Extensions.AsNoTracking()
            .Where(x => x.ExtensionNumber == body.ExtensionNumber)
            .Select(x => new { x.ExtensionNumber, x.DisplayName, x.Password })
            .FirstOrDefaultAsync();

        // Extension passwords are stored in plain text, since Asterisk needs them for the digest
        if (extension is null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(extension.Password), Encoding.UTF8.GetBytes(body.Password)))
        {
            return FailedLogin(ipAddress, "Invalid extension or password");
        }

        LoginAttempts.TryRemove(ipAddress, out _);
        return Ok(new { extension.ExtensionNumber, extension.DisplayName });
    }

    [HttpPost("admin/logout")]
    public async Task<IActionResult> AdminLogout()
    {
        var token = HttpContext.Request.Cookies[AuthenticationMiddleware.TokenCookieName];
        if (token != null)
        {
            await dbContext.AccessTokens.Where(x => x.Token == token).ExecuteDeleteAsync();
            AuthenticationMiddleware.InvalidateToken(token);
        }

        HttpContext.Response.Cookies.Delete(AuthenticationMiddleware.TokenCookieName);
        HttpContext.Response.Cookies.Delete("echopbx_username");
        return Ok("Logout successful");
    }

    [HttpPut("admin/password"), RequireAdmin]
    public async Task<IActionResult> ChangePassword([FromBody] NewPasswordBody body)
    {
        var token = HttpContext.Request.Cookies[AuthenticationMiddleware.TokenCookieName]!;
        var adminId = await dbContext.AccessTokens.Where(x => x.Token == token).Select(x => x.AdminId).FirstOrDefaultAsync();
        if (adminId == 0)
        {
            return Unauthorized("Invalid token");
        }

        var hashed = BCrypt.Net.BCrypt.HashPassword(body.Password);
        await dbContext.Admins.Where(x => x.Id == adminId).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.PasswordHash, hashed)
        );

        // Sign out every other session, since whoever else holds a token may be why the password changed
        var otherTokens = await dbContext.AccessTokens
            .Where(x => x.AdminId == adminId && x.Token != token)
            .Select(x => x.Token)
            .ToArrayAsync();

        await dbContext.AccessTokens.Where(x => otherTokens.Contains(x.Token)).ExecuteDeleteAsync();
        foreach (var otherToken in otherTokens)
        {
            AuthenticationMiddleware.InvalidateToken(otherToken);
        }

        return Ok("Password changed successfully");
    }

    private static bool IsBlacklisted(string ipAddress)
    {
        return BlacklistedIPs.TryGetValue(ipAddress, out var blacklistTime) && DateTime.UtcNow - blacklistTime < BlacklistDuration;
    }

    /// <summary>
    /// Count a failed login, and blacklist the IP address once it has failed too often.
    /// </summary>
    private IActionResult FailedLogin(string ipAddress, string message)
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

        return Unauthorized(message);
    }
}

public record NewPasswordBody(string Password);