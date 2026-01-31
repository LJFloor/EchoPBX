using EchoPBX.Data.Models;

namespace EchoPBX.Data.Services.Asterisk.Models;

public record OngoingCall
{
    public required string UniqueId { get; set; }

    public required string ExternalNumber { get; set; }
    
    public string? ExternalName { get; set; }

    public int? ExtensionNumber { get; set; }
    
    public string? ExtensionName { get; set; }
    
    public int? QueueId { get; set; }
    
    public int? TrunkId { get; set; }

    /// <remarks>In milliseconds</remarks>
    public long StartTime { get; set; }
    
    public long? PickupTime { get; set; }

    public CallDirection Direction { get; set; }

    public CallState State { get; set; }
}

public enum CallState
{
    Ringing = 1,
    Ongoing = 2
}