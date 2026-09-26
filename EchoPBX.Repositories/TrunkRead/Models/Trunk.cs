using EchoPBX.Data.Models;

namespace EchoPBX.Repositories.TrunkRead.Models;

public class Trunk
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Host { get; set; } = null!;
    
    public string Username { get; set; } = null!;
    
    public string Password { get; set; } = null!;
    
    public string[] Codecs { get; set; } = [];
    
    public string? Cid { get; set; }
    
    public bool Connected { get; set; }

    public List<int> Extensions { get; set; } = [];
    
    public int? QueueId { get; set; }

    public IncomingCallBehaviour IncomingCallBehaviour { get; set; }

    public int? CallFlowId { get; set; }
}