using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterApp.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [MaxLength(100)]
    [Column("first_name")]
    public string? FirstName {get; set;}

    [MaxLength(100)]
    [Column("last_name")]
    public string? LastName {get; set;}

    [MaxLength(100)]
    [Column("username")]
    public string? Username { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    [Column("email")]
    public string Email {get; set;} = string.Empty;

    [Required]
    [Column("password_hash")]    
    public string PasswordHash {get; set;} = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}