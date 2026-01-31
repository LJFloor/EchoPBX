using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("access_tokens")]
public class AccessToken
{
    /// <summary>
    /// The ID of the access token
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The token itself
    /// </summary>
    [MaxLength(128)]
    public required string Token { get; set; }
    
    /// <summary>
    /// The expiration time of the access token
    /// </summary>
    /// <remarks>In Unix Timestamp seconds</remarks>
    public long ExpiresAt { get; set; }
    
    public int AdminId { get; set; }
    public Admin Admin { get; set; } = null!;
}