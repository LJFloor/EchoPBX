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

    /// <summary>
    /// The names of the trunks that send their incoming calls to this flow. Only filled for a
    /// single flow.
    /// </summary>
    public string[] Trunks { get; set; } = [];

    public CallFlowDefinition Definition { get; set; } = new();
}
