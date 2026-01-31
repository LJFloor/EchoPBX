using System.Net.Sockets;
using System.Text.Json;
using EchoPBX.Data.Clients.Ami.Models;
using Microsoft.Extensions.Logging;

namespace EchoPBX.Data.Clients.Ami;

public class AmiClient(ILogger<AmiClient> logger) : IAmiClient
{
    /// <summary>
    /// Underlying TCP client for AMI connection
    /// </summary>
    private readonly TcpClient _client = new TcpClient();

    /// <inheritdoc />
    public bool IsConnected => _client.Connected;

    /// <summary>
    /// The username for the AMI server
    /// </summary>
    public const string Username = "echopbx";

    /// <summary>
    /// The password for the AMI server
    /// </summary>
    public const string Secret = "echopbx";

    /// <summary>
    /// The port the AMI server is listening on
    /// </summary>
    public const ushort Port = 5038;

    private StreamReader? _reader;
    private StreamWriter? _writer;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Connecting to Ami server on port {Port}", Port);
        await _client.ConnectAsync("127.0.0.1", Port, cancellationToken);
        logger.LogDebug("Connected to Ami server");

        var stream = _client.GetStream();
        _writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\r\n" };
        _reader = new StreamReader(stream);

        // Read welcome message
        var welcome = await _reader.ReadLineAsync(cancellationToken);
        logger.LogDebug("Received welcome message from Ami server: {Welcome}", welcome);

        // Send login action
        logger.LogDebug("Sending login action to Ami server");
        await _writer.WriteAsync($"Action: Login\r\nUsername: {Username}\r\nSecret: {Secret}\r\n\r\n");

        logger.LogDebug("Subscribing to call events");
        await _writer.WriteAsync("Action: Events\r\nEventMask: call\r\n\r\n");
        await _writer.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AmiEvent> ReadNextEventAsync()
    {
        if (!IsConnected || _reader == null)
        {
            throw new InvalidOperationException("Not connected to AMI server");
        }

        var eventData = new Dictionary<string, string>();

        while (IsConnected)
        {
            var line = await _reader.ReadLineAsync();

            if (line == null)
            {
                await Task.Delay(100);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (eventData.Count > 0)
                {
                    // Process event
                    if (eventData.TryGetValue("Event", out var eventType))
                    {
                        // indented
                        var jsonOption = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        
                        var jsonData = JsonSerializer.Serialize(eventData, jsonOption);
                        
                        logger.LogDebug("Received AMI event of type {EventType} with data:\n{EventData}", eventType, jsonData);
                        var amiEvent = new AmiEvent(eventData);
                        eventData.Clear();
                        return amiEvent;
                    }
                }

                continue;
            }

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex > 0)
            {
                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                eventData[key] = value;
            }
        }
        
        logger.LogCritical("Disconnected from AMI server while reading events");

        throw new InvalidOperationException("Disconnected from AMI server");
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        _client.Close();
    }
}