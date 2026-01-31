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
    /// Path to the DTMF menu announcement audio file (without .wav extension).
    /// </summary>
    public string? DtmfAnnouncement { get; set; }

    public List<DtmfMenuEntry> DtmfMenuEntries { get; set; } = [];
}

public enum IncomingCallBehaviour
{
    Ignore = 1,
    RingAllExtensions = 2,
    RingSpecificExtensions = 3,
    SendToQueue = 4,
    DtmfMenu = 5
}