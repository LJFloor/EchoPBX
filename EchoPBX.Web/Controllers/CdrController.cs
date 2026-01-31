using EchoPBX.Repositories.CdrRead;
using EchoPBX.Repositories.CdrRead.Models;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EchoPBX.Web.Controllers;

/// <summary>
/// Controller for Call Detail Records (CDR).
/// </summary>
[ApiController, RequireAdmin, Route("/api/cdr")]
public class CdrController(ICdrReadRepository cdrReadRepository)
{
    /// <summary>
    /// List CDR entries.
    /// </summary>
    /// <param name="n">Number of entries to return</param>
    public async Task<CdrEntry[]> List([FromQuery] int n = 100)
    {
        return await cdrReadRepository.List(n);
    }
}