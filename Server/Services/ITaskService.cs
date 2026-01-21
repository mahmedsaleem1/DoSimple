using Server.DTOs;
using Server.Models;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.Services;

public interface ITaskService
{
    // Create
    Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request, int createdByUserId);
    
    // Read
    Task<TaskResponse?> GetTaskByIdAsync(int taskId, int userId, string userRole);
    Task<TaskListResponse> GetTasksAsync(
        int userId, 
        string userRole, 
        TaskStatus? status = null, 
        TaskPriority? priority = null,
        string? category = null,
        int? assignedToUserId = null,
        bool? isOverdue = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        string? searchTerm = null,
        int pageNumber = 1, 
        int pageSize = 10);
    Task<TaskStatsResponse> GetTaskStatsAsync(int userId, string userRole);
    Task<List<string>> GetCategoriesAsync(int userId, string userRole);
    
    // Update
    Task<TaskResponse?> UpdateTaskAsync(int taskId, UpdateTaskRequest request, int userId, string userRole);
    Task<TaskResponse?> UpdateTaskStatusAsync(int taskId, TaskStatus status, int userId, string userRole);
    Task<TaskResponse?> AssignTaskAsync(int taskId, int assignToUserId, int userId, string userRole);
    Task<TaskResponse?> UnassignTaskAsync(int taskId, int userId, string userRole);
    
    // Delete
    Task<bool> DeleteTaskAsync(int taskId, int userId, string userRole);
    
    // Bulk Operations
    Task<bool> BulkDeleteTasksAsync(List<int> taskIds, int userId, string userRole);
    Task<bool> BulkUpdateStatusAsync(List<int> taskIds, TaskStatus status, int userId, string userRole);
}
