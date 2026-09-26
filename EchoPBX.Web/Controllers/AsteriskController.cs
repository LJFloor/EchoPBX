using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using EchoPBX.Data.Clients.Ami;
using EchoPBX.Data.Services.Asterisk;
using EchoPBX.Data.Services.Asterisk.Models;
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
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        using var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        // Only the latest list matters, so a slow client skips updates instead of queueing them
        var updates = Channel.CreateBounded<List<OngoingCall>>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
        EventHandler<List<OngoingCall>> onUpdated = (_, calls) => updates.Writer.TryWrite(calls);
        asterisk.OngoingCallsUpdated += onUpdated;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        var sending = SendUpdates(cts.Token);
        var receiving = WaitForClose(cts.Token);

        try
        {
            // Either the client went away or sending failed; both end the connection
            await Task.WhenAny(sending, receiving);
            await cts.CancelAsync();
            await Task.WhenAll(sending, receiving);
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            // Usually caused by client disconnects
            logger.LogDebug("Ongoing calls live websocket closed: {Message}", ex.Message);
        }
        finally
        {
            asterisk.OngoingCallsUpdated -= onUpdated;
        }

        if (websocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await websocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // Already gone
            }
        }

        return;

        async Task SendUpdates(CancellationToken cancellationToken)
        {
            await foreach (var calls in updates.Reader.ReadAllAsync(cancellationToken))
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(calls, jsonOptions);
                await websocket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        // The client never sends anything, but reading is the only way to notice it closing
        async Task WaitForClose(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await websocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) return;
            }
        }
    }
}