using System.ComponentModel.DataAnnotations;
using EchoPBX.Data.Services.CallFlows;
using EchoPBX.Repositories.CallFlowRead;
using EchoPBX.Repositories.CallFlowWrite;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EchoPBX.Web.Controllers;

/// <summary>
/// Controller for managing call flows.
/// </summary>
[ApiController, RequireAdmin, Route("api/call-flows")]
public class CallFlowController(
    ICallFlowReadRepository callFlowReadRepository,
    ICallFlowWriteRepository callFlowWriteRepository) : ControllerBase
{
    /// <summary>
    /// List all call flows.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var callFlows = await callFlowReadRepository.List();
        return Ok(callFlows);
    }

    /// <summary>
    /// Get a call flow by its slug.
    /// </summary>
    /// <param name="slug">The slug of the call flow.</param>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var callFlow = await callFlowReadRepository.GetBySlug(slug);
        return callFlow is null ? NotFound() : Ok(callFlow);
    }

    /// <summary>
    /// Create a new call flow.
    /// </summary>
    /// <param name="callFlow">The call flow to create.</param>
    /// <remarks>The request size limit is set to 200 MB to accommodate the base64 of uploaded sounds.</remarks>
    [HttpPost, RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Create([FromBody] EchoPBX.Repositories.CallFlowWrite.Models.CallFlow callFlow)
    {
        try
        {
            await callFlowWriteRepository.Create(callFlow);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    /// <summary>
    /// Update an existing call flow.
    /// </summary>
    /// <param name="callFlow">The call flow to update.</param>
    /// <remarks>The request size limit is set to 200 MB to accommodate the base64 of uploaded sounds.</remarks>
    [HttpPut, RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Update([FromBody] EchoPBX.Repositories.CallFlowWrite.Models.CallFlow callFlow)
    {
        try
        {
            await callFlowWriteRepository.Update(callFlow);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a call flow by its ID.
    /// </summary>
    /// <param name="id">The ID of the call flow to delete.</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await callFlowWriteRepository.Delete(id);
        return NoContent();
    }

    /// <summary>
    /// Test the call flow by calling the specified extension and running the flow on it.
    /// </summary>
    [HttpPost("{id:int}/test")]
    public async Task<IActionResult> Test(int id, [FromQuery] int to)
    {
        var callFlow = await callFlowReadRepository.GetById(id);
        if (callFlow is null)
        {
            return NotFound();
        }

        string[] lines =
        [
            $"Channel: PJSIP/{to}",
            $"CallerID: {callFlow.Name} <SYSTEM>",
            "Context: system",
            "MaxRetries: 0",
            "WaitTime: 30",
            "Priority: 1",
            $"Extension: {CallFlowDialplanBuilder.ContextName(callFlow.Slug)}",
        ];

        var tempPath = Path.GetTempFileName() + ".call";
        await System.IO.File.WriteAllLinesAsync(tempPath, lines);
        System.IO.File.Move(tempPath, "/var/spool/asterisk/outgoing/" + Path.GetFileName(tempPath));
        return NoContent();
    }
}
