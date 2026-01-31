using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

[Table("contacts")]
public class Contact
{
    public int Id { get; set; }

    /// <summary>
    /// The name of the contact.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// The phone number of the contact.
    /// </summary>
    [MaxLength(20)]
    public required string PhoneNumber { get; set; }
}