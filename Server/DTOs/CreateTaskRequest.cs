using System.ComponentModel.DataAnnotations;
using Server.Models;

namespace Server.DTOs;

public class CreateTaskRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Required(ErrorMessage = "Category is required")]
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string Category { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    public int? AssignedToUserId { get; set; }
}
