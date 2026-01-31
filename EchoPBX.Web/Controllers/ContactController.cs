using System.Net;
using EchoPBX.Data;
using EchoPBX.Data.Services;
using EchoPBX.Data.Services.ContactSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Web.Controllers;

/// <summary>
/// Controller for contact resolution.
/// </summary>
[ApiController, Route("/api/contacts")]
public class ContactController(IContactSearchService contactSearchService, ILogger<ContactController> logger, EchoDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Resolve a phone number to a contact name.
    /// </summary>
    /// <param name="num">The phone number to resolve.</param>
    /// <remarks>The lookup is performed using the last 7 digits of the phone number to roughly match local numbers.</remarks>
    [HttpGet("lookup")]
    public async Task<string> Resolve([FromQuery] string num)
    {
        if (HttpContext.Connection.RemoteIpAddress == null || !IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress))
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return string.Empty;
        }

        var contact = await contactSearchService.Search(num);
        if (contact == null)
        {
            return string.Empty;
        }

        logger.LogInformation("Incoming phone number {Number} matched to {Name}", num, contact.Name);
        return contact.Name;
    }
}