using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class UpdateUserRequest
{
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    public string? Name { get; set; }

    [EmailAddress]
    [StringLength(50, ErrorMessage = "Email cannot exceed 50 characters")]
    public string? Email { get; set; }

    public bool? IsEmailVerified { get; set; }
}
