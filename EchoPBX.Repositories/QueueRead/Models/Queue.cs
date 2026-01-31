namespace EchoPBX.Repositories.QueueRead.Models;

public class Queue
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Strategy { get; set; } = null!;

    public int Timeout { get; set; }

    public int MaxLength { get; set; }

    public int WrapUpTime { get; set; }

    public int RetryInterval { get; set; }

    public string[] MusicOnHold { get; set; } = [];
    
    public string? Announcement { get; set; }
    
    public List<int> Extensions { get; set; } = [];
}