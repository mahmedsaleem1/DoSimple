using System.ComponentModel.DataAnnotations;
using Server.Models;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.DTOs;

public class UpdateTaskRequest
{
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    public TaskStatus? Status { get; set; }

    public TaskPriority? Priority { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    public DateTime? DueDate { get; set; }

    public int? AssignedToUserId { get; set; }
}
