using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("trunks")]
public class Trunk
{
    [Key] public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Host { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;
    
    public string Codecs { get; set; } = null!;
    
    public string? Cid { get; set; } = null!;
    
    public List<Extension> Extensions { get; set; } = [];
    
    public int? QueueId { get; set; }
    public Queue? Queue { get; set; }

    public IncomingCallBehaviour IncomingCallBehaviour { get; set; } = IncomingCallBehaviour.RingAllExtensions;

    /// <summary>
    /// The flow incoming calls run through when <see cref="IncomingCallBehaviour"/> is
    /// <see cref="IncomingCallBehaviour.SendToCallFlow"/>.
    /// </summary>
    public int? CallFlowId { get; set; }
    public CallFlow? CallFlow { get; set; }
}

public enum IncomingCallBehaviour
{
    Ignore = 1,
    RingAllExtensions = 2,
    RingSpecificExtensions = 3,
    SendToQueue = 4,

    // 5 was a DTMF menu that transferred to queues. Those are call flows now, see the
    // MoveDtmfMenusToCallFlows migration.

    SendToCallFlow = 6,
}