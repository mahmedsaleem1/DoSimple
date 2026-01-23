using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(User|Admin|SuperAdmin)$", ErrorMessage = "Role must be User, Admin, or SuperAdmin")]
    public string Role { get; set; } = string.Empty;
}
