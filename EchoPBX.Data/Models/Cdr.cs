using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("cdr")]
public class Cdr
{
    public int Id { get; set; }

    /// <summary>
    /// Caller ID
    /// </summary>
    [MaxLength(80)]
    public required string Clid { get; set; }

    /// <summary>
    /// Source
    /// </summary>
    /// <example>201</example>
    [MaxLength(80)]
    public required string Source { get; set; }

    /// <summary>
    /// Destination
    /// </summary>
    /// <example>202</example>
    [MaxLength(80)]
    public required string Destination { get; set; }

    /// <summary>
    /// DestinationContext
    /// </summary>
    /// <example>using-trunk-1</example>
    [MaxLength(80)]
    public required string DestinationContext { get; set; }

    /// <summary>
    /// Channel name
    /// </summary>
    /// <example>"Ronaldo" &lt;202&gt;</example>
    [MaxLength(80)]
    public required string ChannelName { get; set; }

    /// <summary>
    /// Destination channel
    /// </summary>
    /// <example>PJSIP/201-00000001</example>
    [MaxLength(80)]
    public required string DestinationChannel { get; set; }

    /// <summary>
    /// Last app executed
    /// </summary>
    /// <example>Dial</example>
    [MaxLength(80)]
    public required string LastAppExecuted { get; set; }

    /// <summary>
    /// Last app's arguments
    /// </summary>
    /// <example>PJSIP/201</example>
    [MaxLength(80)]
    public required string LastAppArguments { get; set; }

    /// <summary>
    /// Time the call was started.
    /// </summary>
    /// <remarks>In Unix Timestamp Milliseconds</remarks>
    public long Start { get; set; }

    /// <summary>
    /// Time the call was answered.
    /// </summary>
    /// <remarks>In Unix Timestamp Milliseconds</remarks>
    public long? Answer { get; set; }

    /// <summary>
    /// Time the call was ended
    /// </summary>
    /// <remarks>In Unix Timestamp Milliseconds</remarks>
    public long End { get; set; }

    /// <summary>
    /// Total duration of the call (Ringing + Answer)
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Duration of the call once it was answered
    /// </summary>
    public int BillSeconds { get; set; }

    /// <summary>
    /// Disposition (ANSWERED, NO ANSWER, BUSY)
    /// </summary>
    public CdrDisposition Disposition { get; set; }
    
    /// <summary>
    /// Ama flags (DOCUMENTATION, BILL, IGNORE etc)
    /// </summary>
    [MaxLength(80)]
    public required string AmaFlags { get; set; }
    
    /// <summary>
    /// The direction of the call
    /// </summary>
    public CallDirection Direction { get; set; }
}

public enum CdrDisposition
{
    Answered = 1,
    NoAnswer = 2,
    Busy = 3,
}

public enum CallDirection
{
    Incoming = 1,
    Outgoing = 2,
    Internal = 3,
}