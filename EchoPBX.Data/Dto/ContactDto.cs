namespace EchoPBX.Data.Dto;

public struct ContactDto
{
    public string Endpoint { get; set; }
    
    public string Host { get; set; }
    
    public ContactStatus Status { get; set; }
}

public enum ContactStatus
{
    Available = 1,
    NonQualify = 2,
    Unavailable = 3,
}