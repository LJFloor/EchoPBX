using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("extensions")]
public class Extension
{
    /// <summary>
    /// The extension number
    /// </summary>
    /// <example>201</example>
    [Key]
    public int ExtensionNumber { get; set; }

    /// <summary>
    /// Password for the extension
    /// </summary>
    [MaxLength(128)]
    public string Password { get; set; } = null!;

    /// <summary>
    /// Display name for the extension
    /// </summary>
    /// <example>John Smith</example>
    [MaxLength(32)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The maximum number of contacts (devices) that can be registered to this extension
    /// </summary>
    [DefaultValue(1)]
    public int MaxDevices { get; set; } = 5;

    public Trunk? OutgoingTrunk { get; set; }
    public int? OutgoingTrunkId { get; set; }

    public List<QueueExtension> Queues { get; set; } = [];
}