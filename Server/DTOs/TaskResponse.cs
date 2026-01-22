using Server.Models;

namespace Server.DTOs;

public class TaskResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? ImageUrl { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
