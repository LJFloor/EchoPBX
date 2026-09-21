using EchoPBX.Data.Models;

namespace EchoPBX.Repositories.CallFlowRead.Models;

public class CallFlow
{
    public int Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? InternalNumber { get; set; }

    /// <summary>
    /// The number of steps in the flow, excluding the start card. Shown in the overview.
    /// </summary>
    public int Steps { get; set; }

    public CallFlowDefinition Definition { get; set; } = new();
}
