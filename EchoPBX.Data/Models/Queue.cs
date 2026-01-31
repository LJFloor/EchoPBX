using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("queues")]
public class Queue
{
    /// <summary>
    /// The ID of the queue.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the queue.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The strategy Asterisk uses to distribute calls among agents.<br />
    /// - ringall: Rings all available agents until one answers.<br />
    /// - roundrobin: Rings agents in a round-robin fashion.<br />
    /// - leastrecent: Rings the agent who has not taken a call for the longest time.<br />
    /// - fewestcalls: Rings the agent who has taken the fewest calls.<br />
    /// - random: Rings a random available agent.<br />
    /// </summary>
    /// <remarks>Default is "ringall"</remarks>
    public string Strategy { get; set; } = "ringall";

    /// <summary>
    /// How long Asterisk rings an agent before giving up and trying the next one.
    /// </summary>
    public int Timeout { get; set; } = 15;

    /// <summary>
    /// The maximum number of callers allowed waiting in the queue.
    /// </summary>
    public int MaxLength { get; set; } = 0;

    /// <summary>
    /// How long Asterisk waits before trying an agent again after the timeout. <seealso cref="Timeout"/>
    /// </summary>
    public int RetryInterval { get; set; } = 5;
    
    /// <summary>
    /// The music on hold class to use for callers waiting in the queue. If null, the default MOH class is used.
    /// </summary>
    public string? MusicOnHold { get; set; }
    
    /// <summary>
    /// An announcement to play to callers when they enter the queue. If null, no announcement is played.
    /// </summary>
    public string? Announcement { get; set; }

    /// <summary>
    /// How long an agent gets after finishing a call before being eligible for another call.
    /// </summary>
    public int WrapUpTime { get; set; } = 0;

    /// <summary>
    /// List of extensions that are static members of this queue.
    /// </summary>
    public List<QueueExtension> Extensions { get; set; } = [];
}