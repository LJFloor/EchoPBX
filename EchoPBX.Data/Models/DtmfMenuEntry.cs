using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("dtmf_menu_entries")]
public class DtmfMenuEntry
{
    public int TrunkId { get; set; }
    public Trunk Trunk { get; set; } = null!;

    /// <summary>
    /// The DTMF digit (0-9) that triggers this entry.
    /// </summary>
    public int Digit { get; set; }

    public int QueueId { get; set; }
    public Queue Queue { get; set; } = null!;
}
