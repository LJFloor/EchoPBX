using EchoPBX.Data.Clients.Ami.Models;

namespace EchoPBX.Data.Clients.Ami;

/// <summary>
/// Client for interacting with the Asterisk Manager Interface (AMI).
/// Connects to the AMI server, logs in, subscribes to call events, and processes them.
/// </summary>
public interface IAmiClient
{
    /// <summary>
    /// Connect to the AMI server
    /// </summary>
    public Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Data reader event for when an AMI event is received
    /// </summary>
    public Task<AmiEvent> ReadNextEventAsync();

    /// <summary>
    /// Disconnect from the AMI server
    /// </summary>
    public void Disconnect();

    /// <summary>
    /// Whether the client is connected to the AMI server
    /// </summary>
    public bool IsConnected { get; }
}