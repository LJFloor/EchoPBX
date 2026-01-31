using System.Net;

namespace EchoPBX.Data.Clients.Stun;

/// <summary>
/// Interface for a STUN client to discover public IP address.
/// </summary>
public interface IStunClient
{
    /// <summary>
    /// Configures the STUN server and port.
    /// </summary>
    /// <param name="stunServer">The STUN server address. Can be a hostname or an ip address</param>
    /// <param name="stunPort">>The STUN server port.</param>
    public void Configure(string stunServer, ushort stunPort);

    /// <summary>
    /// Gets the public IP address using STUN protocol.
    /// </summary>
    /// <returns>The public IP address if found; otherwise, null.</returns>
    public Task<IPAddress?> GetPublicIpAddress();
}