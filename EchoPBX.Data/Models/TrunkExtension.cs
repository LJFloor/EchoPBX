using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("trunk_extensions")]
public class TrunkExtension
{
    public Trunk Trunk { get; set; } = null!;
    public int TrunkId { get; set; }

    public Extension Extension { get; set; } = null!;
    public int ExtensionNumber { get; set; }
}