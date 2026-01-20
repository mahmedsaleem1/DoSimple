using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class RegisterAdminRequest
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string AdminSecretKey { get; set; } = string.Empty;
}
