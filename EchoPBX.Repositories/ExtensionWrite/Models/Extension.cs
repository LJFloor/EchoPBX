namespace EchoPBX.Repositories.ExtensionWrite.Models;

public class Extension
{
    public int ExtensionNumber { get; set; }

    public required string Password { get; set; }

    public string? DisplayName { get; set; }

    public int MaxDevices { get; set; }

    public int? OutgoingTrunkId { get; set; }
}