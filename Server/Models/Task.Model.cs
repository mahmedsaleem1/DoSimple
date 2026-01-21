namespace Server.Models;

public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string Category { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    
    // Foreign Keys
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    
    // Navigation Properties
    public User CreatedByUser { get; set; } = null!;
    public User? AssignedToUser { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
