using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class AssignTaskRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }
}
