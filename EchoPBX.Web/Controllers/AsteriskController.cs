using EchoPBX.Data.Clients.Ami;
using EchoPBX.Data.Services.Asterisk;
using EchoPBX.Data.Workers.Asterisk;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EchoPBX.Web.Controllers;

/// <summary>
/// Controller for Asterisk-related operations.
/// </summary>
[ApiController, RequireAdmin, Route("/api/asterisk")]
public class AsteriskController(IAsteriskWorker asterisk, ILogger<AsteriskController> logger) : ControllerBase
{
    /// <summary>
    /// Reload Asterisk configuration.
    /// </summary>
    [HttpPost("reload")]
    public async Task<IActionResult> Reload()
    {
        await asterisk.ApplyChanges();
        return NoContent();
    }

    /// <summary>
    /// Translation layer that allows you to communicate with the Asterisk Manager Interface (AMI).
    /// </summary>
    [Route("ami")]
    public async Task Ami([FromServices] IAmiClient amiClient)
    {
        await amiClient.ConnectAsync();

        // websockets baby
        var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        try
        {
            while (websocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var amiEvent = await amiClient.ReadNextEventAsync();
                var json = System.Text.Json.JsonSerializer.Serialize(amiEvent);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);
                await websocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, HttpContext.RequestAborted);
            }
        }
        catch (Exception ex)
        {
            // Ignore exceptions, usually caused by client disconnects
            logger.LogWarning("Error in AMI websocket: {Message}", ex.Message);
        }
        finally
        {
            amiClient.Disconnect();
        }
    }

    /// <summary>
    /// Get a list of ongoing calls in Asterisk.
    /// </summary>
    [HttpGet("ongoing-calls")]
    public IActionResult GetOngoingCalls()
    {
        return Ok(asterisk.OngoingCalls);
    }

    /// <summary>
    /// Get live updates of ongoing calls via WebSocket.
    /// </summary>
    [HttpGet("ongoing-calls/live")]
    public async Task GetOngoingCallsLive()
    {
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };

        try
        {
            var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            asterisk.OngoingCallsUpdated += async (s, calls) =>
            {
                if (websocket.State != System.Net.WebSockets.WebSocketState.Open)
                {
                    return;
                }

                var json = System.Text.Json.JsonSerializer.Serialize(calls, jsonOptions);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);
                await websocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            };
            while (websocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                await Task.Delay(10);
            }
        }
        catch (Exception ex)
        {
            // Ignore exceptions, usually caused by client disconnects
            logger.LogWarning("Error in ongoing calls live websocket: {Message}", ex.Message);
        }
    }
}