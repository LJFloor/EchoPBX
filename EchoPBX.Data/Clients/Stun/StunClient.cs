using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace EchoPBX.Data.Clients.Stun;

public class StunClient(ILogger<StunClient> logger) : IStunClient
{
    private string _stunServer = "stun.l.google.com";
    private ushort _stunPort = 19302;
    private IPAddress? _lastPublicIp;

    public void Configure(string stunServer, ushort stunPort)
    {
        _stunServer = stunServer;
        _stunPort = stunPort;
    }

    public async Task<IPAddress?> GetPublicIpAddress()
    {
        try
        {
            using var udpClient = new UdpClient();
            udpClient.Client.SendTimeout = 1000;
            udpClient.Client.ReceiveTimeout = 5000;

            // STUN Binding Request (20 bytes)
            var stunRequest = new byte[20];
            stunRequest[0] = 0x00;
            stunRequest[1] = 0x01; // Binding Request
            stunRequest[2] = 0x00;
            stunRequest[3] = 0x00; // Message Length
            stunRequest[4] = 0x21;
            stunRequest[5] = 0x12; // Magic Cookie
            stunRequest[6] = 0xA4;
            stunRequest[7] = 0x42;

            // Transaction ID (random 12 bytes)
            var random = new Random();
            random.NextBytes(stunRequest.AsSpan(8, 12));

            // If an ip address is provided, parse it, otherwise resolve hostname
            if (!IPAddress.TryParse(_stunServer, out var stunServerIp))
            {
                stunServerIp = (await Dns.GetHostAddressesAsync(_stunServer))[0];
            }

            var endpoint = new IPEndPoint(stunServerIp, _stunPort);
            await udpClient.SendAsync(stunRequest, stunRequest.Length, endpoint);

            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await udpClient.ReceiveAsync(tokenSource.Token);
            var data = response.Buffer;

            // Parse XOR-MAPPED-ADDRESS (attribute type 0x0020)
            for (var i = 20; i < data.Length;)
            {
                var attrType = (ushort)((data[i] << 8) | data[i + 1]);
                var attrLen = (ushort)((data[i + 2] << 8) | data[i + 3]);

                if (attrType == 0x0020) // XOR-MAPPED-ADDRESS
                {
                    var family = data[i + 5];
                    if (family == 0x01) // IPv4
                    {
                        // XOR with magic cookie
                        var ipBytes = new byte[4];
                        ipBytes[0] = (byte)(data[i + 8] ^ 0x21);
                        ipBytes[1] = (byte)(data[i + 9] ^ 0x12);
                        ipBytes[2] = (byte)(data[i + 10] ^ 0xA4);
                        ipBytes[3] = (byte)(data[i + 11] ^ 0x42);

                        var result = new IPAddress(ipBytes);
                        _lastPublicIp = result;
                        return result;
                    }
                }

                i += 4 + attrLen;
                i = (i + 3) & ~3; // Padding to 4-byte boundary
            }
        }
        catch
        {
            // Ignore exceptions and return null
        }


        if (_lastPublicIp != null)
        {
            logger.LogWarning("Using last known public IP address {PublicIp} due to STUN failure", _lastPublicIp);
        }
        else
        {
            logger.LogError("Failed to obtain public IP address from STUN server {StunServer}:{StunPort}", _stunServer, _stunPort);
        }

        return _lastPublicIp;
    }
}