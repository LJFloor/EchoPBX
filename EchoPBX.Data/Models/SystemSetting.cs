using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("system_settings")]
public class SystemSetting
{
    /// <summary>
    /// The unique identifier for the system setting. Not really used.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The name of the setting.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// The value of the setting.
    /// </summary>
    [MaxLength(1000)]
    public required string Value { get; set; }
}