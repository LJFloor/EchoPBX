using EchoPBX.Repositories.TrunkRead;
using EchoPBX.Repositories.TrunkWrite;
using EchoPBX.Repositories.TrunkWrite.Models;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EchoPBX.Web.Controllers;

/// <summary>
/// Controller for Trunks.
/// </summary>
[ApiController, RequireAdmin, Route("/api/trunks")]
public class TrunkController(ITrunkReadRepository trunkReadRepository, ITrunkWriteRepository trunkWriteRepository) : ControllerBase
{
    /// <summary>
    /// List all trunks
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var trunks = await trunkReadRepository.List();
        return Ok(trunks);
    }

    /// <summary>
    /// Get a single trunk by ID
    /// </summary>
    /// <param name="id">The ID of the trunk to get</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTrunk(int id)
    {
        var trunk = await trunkReadRepository.Get(id);
        return trunk == null ? NotFound() : Ok(trunk);
    }

    /// <summary>
    /// Create a new trunk
    /// </summary>
    /// <param name="trunk">The trunk to create</param>
    [HttpPost]
    [RequestSizeLimit(20_000_000)] // 20MB for base64 audio uploads
    public async Task<IActionResult> CreateTrunk([FromBody] Trunk trunk)
    {
        var id = await trunkWriteRepository.Create(trunk);
        return NoContent();
    }

    /// <summary>
    /// Update an existing trunk
    /// </summary>
    /// <param name="trunk">The trunk to update</param>
    [HttpPut]
    [RequestSizeLimit(20_000_000)] // 20MB for base64 audio uploads
    public async Task<IActionResult> UpdateTrunk([FromBody] Trunk trunk)
    {
        await trunkWriteRepository.Update(trunk);
        return NoContent();
    }

    /// <summary>
    /// Delete a trunk by ID
    /// </summary>
    /// <param name="id">The ID of the trunk to delete</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTrunk(int id)
    {
        await trunkWriteRepository.Delete(id);
        return NoContent();
    }
}