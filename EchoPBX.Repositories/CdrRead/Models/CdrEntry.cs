using EchoPBX.Data.Models;

namespace EchoPBX.Repositories.CdrRead.Models;

public class CdrEntry
{
    public int Id { get; set; }

    public required string Source { get; set; }

    public required string Destination { get; set; }

    public CallDirection Direction { get; set; }
    
    public long Start { get; set; }
    
    public long? Answer { get; set; }
    
    public long End { get; set; }
    
    public int Duration { get; set; }
}