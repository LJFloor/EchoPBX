using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

/// <summary>
/// Represents an admin user
/// </summary>
[Table("admins")]
public class Admin
{
    /// <summary>
    /// The ID of the admin user.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The username of the admin user.
    /// </summary>
    /// <example>Admin</example>
    [MaxLength(50)]
    public required string Username { get; set; }

    /// <summary>
    /// The bcrypt hash of the admin user's password.
    /// </summary>
    [MaxLength(100)]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Sets the password for the admin user by hashing it using bcrypt.
    /// </summary>
    /// <param name="password">The password to set.</param>
    public void SetPassword(string password)
    {
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies the provided password against the stored bcrypt hash.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }
}