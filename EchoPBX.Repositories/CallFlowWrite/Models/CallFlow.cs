using EchoPBX.Data.Models;

namespace EchoPBX.Repositories.CallFlowWrite.Models;

public class CallFlow
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? InternalNumber { get; set; }

    public CallFlowDefinition Definition { get; set; } = new();
}
