using EchoPBX.Repositories.QueueRead;
using EchoPBX.Repositories.QueueWrite;
using EchoPBX.Repositories.QueueWrite.Models;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EchoPBX.Web.Controllers;

/// <summary>
/// Controller for managing queues.
/// </summary>
[ApiController, RequireAdmin, Route("api/queues")]
public class QueueController(IQueueReadRepository queueReadRepository, IQueueWriteRepository queueWriteRepository) : ControllerBase
{
    /// <summary>
    /// List all queues.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var queues = await queueReadRepository.List();
        return Ok(queues);
    }

    /// <summary>
    /// Get a queue by its ID.
    /// </summary>
    /// <param name="id">The ID of the queue.</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var queue = await queueReadRepository.GetById(id);
        return queue is null ? NotFound() : Ok(queue);
    }

    /// <summary>
    /// Create a new queue.
    /// </summary>
    /// <param name="queue">The queue to create.</param>
    /// <remarks>The request size limit is set to 200 MB to accommodate the base64 of hold music.</remarks>
    [HttpPost, RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Create([FromBody] Queue queue)
    {
        await queueWriteRepository.Create(queue);
        return NoContent();
    }

    /// <summary>
    /// Update an existing queue.
    /// </summary>
    /// <param name="queue">The queue to update.</param>
    /// <remarks>The request size limit is set to 200 MB to accommodate the base64 of hold music.</remarks>
    [HttpPut, RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Update([FromBody] Queue queue)
    {
        await queueWriteRepository.Update(queue);
        return NoContent();
    }
    
    /// <summary>
    /// Delete a queue by its ID.
    /// </summary>
    /// <param name="id">The ID of the queue to delete.</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await queueWriteRepository.Delete(id);
        return NoContent();
    }

    /// <summary>
    /// Test the queue by calling the specified extension and connecting it to the queue.
    /// </summary>
    [HttpPost("{id:int}/test")]
    public async Task<IActionResult> Locate(int id, [FromQuery] int to)
    {
        var queue = await queueReadRepository.GetById(id);
        if (queue is null)
        {
            return NotFound();
        }

        string[] lines =
        [
            $"Channel: PJSIP/{to}",
            $"CallerID: {queue.Name} <SYSTEM>",
            "Context: system",
            "MaxRetries: 0",
            "WaitTime: 30",
            "Priority: 1",
            $"Extension: queue-{id}",
        ];

        var tempPath = Path.GetTempFileName() + ".call";
        await System.IO.File.WriteAllLinesAsync(tempPath, lines);
        System.IO.File.Move(tempPath, "/var/spool/asterisk/outgoing/" + Path.GetFileName(tempPath));
        return NoContent();
    }
}