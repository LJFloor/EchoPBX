using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("queue_extensions")]
public class QueueExtension
{
    public Queue Queue { get; set; } = null!;
    public int QueueId { get; set; }

    public Extension Extension { get; set; } = null!;
    public int ExtensionNumber { get; set; }

    /// <summary>
    /// The position of the extension in the queue
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Whether the extension is enabled in the queue
    /// </summary>
    public bool Enabled { get; set; }
}