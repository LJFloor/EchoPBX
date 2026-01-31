using System.Collections.Concurrent;
using System.Net;
using EchoPBX.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EchoPBX.Web.Authentication;

/// <summary>
/// Middleware for handling authentication via cookies.
/// </summary>
public class AuthenticationMiddleware : IMiddleware
{
    /// <summary>
    /// The name of the cookie used to store the authentication token.
    /// </summary>
    public const string TokenCookieName = "echopbx_token";

    /// <summary>
    /// A cache mapping authentication tokens to admin IDs.
    /// </summary>
    private static readonly MemoryCache TokenCache = new MemoryCache(new MemoryCacheOptions
    {
        ExpirationScanFrequency = TimeSpan.FromMinutes(5)
    });

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await TrySetAdminId(context);

        var requireAdminAttribute = context.GetEndpoint()?.Metadata.GetMetadata<RequireAdminAttribute>();
        if (requireAdminAttribute is not null && !context.Items.ContainsKey("AdminId"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Attempts to set the AdminId in the HttpContext.Items based on the authentication token cookie.
    /// </summary>
    private static async Task TrySetAdminId(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(TokenCookieName, out var token) || string.IsNullOrEmpty(token))
        {
            return;
        }

        if (TokenCache.TryGetValue(token, out var adminId))
        {
            context.Items["AdminId"] = adminId;
            return;
        }

        var dbContext = context.RequestServices.GetRequiredService<EchoDbContext>();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var accessToken = await dbContext.AccessTokens
            .Where(x => x.Token == token && x.ExpiresAt > now)
            .Select(x => new { x.AdminId, x.ExpiresAt })
            .FirstOrDefaultAsync();

        if (accessToken is not null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(accessToken.ExpiresAt)
            };

            context.Items["AdminId"] = accessToken.AdminId;
        }
    }
}