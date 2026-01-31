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

    public string? DtmfAnnouncement { get; set; }

    public List<DtmfMenuEntryDto> DtmfMenuEntries { get; set; } = [];
}

public class DtmfMenuEntryDto
{
    public int Digit { get; set; }
    public int QueueId { get; set; }
}