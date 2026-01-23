using System.ComponentModel.DataAnnotations;
using Server.Models;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.DTOs;

public class UpdateTaskStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    public TaskStatus Status { get; set; }
}
