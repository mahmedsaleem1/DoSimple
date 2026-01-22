using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(AppDbContext context, ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== CREATE ====================
    
    public async Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request, int createdByUserId)
    {
        try
        {
            // Validate assigned user exists if provided
            if (request.AssignedToUserId.HasValue)
            {
                var assignedUser = await _context.Users.FindAsync(request.AssignedToUserId.Value);
                if (assignedUser == null)
                {
                    _logger.LogWarning("Cannot assign task to non-existent user {UserId}", request.AssignedToUserId.Value);
                    return null;
                }
            }

            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Category = request.Category,
                DueDate = request.DueDate,
                Status = TaskStatus.Pending,
                CreatedByUserId = createdByUserId,
                AssignedToUserId = request.AssignedToUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} created by user {UserId}", task.Id, createdByUserId);

            return await GetTaskByIdAsync(task.Id, createdByUserId, "User");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task for user {UserId}", createdByUserId);
            return null;
        }
    }

    // ==================== READ ====================
    
    public async Task<TaskResponse?> GetTaskByIdAsync(int taskId, int userId, string userRole)
    {
        try
        {
            var query = _context.Tasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .AsQueryable();

            // Apply authorization filter
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                query = query.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);
            }

            var task = await query.FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found or unauthorized access by user {UserId}", taskId, userId);
                return null;
            }

            return MapToTaskResponse(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {TaskId} for user {UserId}", taskId, userId);
            return null;
        }
    }

    public async Task<TaskListResponse> GetTasksAsync(
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
        int pageSize = 10,
        int? createdByUserIdFilter = null)
    {
        try
        {
            var query = _context.Tasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .AsQueryable(); // Dont make db request now ill add more WHERE's too

            // Apply authorization filter
            if (createdByUserIdFilter.HasValue)
            {
                // Filter only by tasks created by specific user
                query = query.Where(t => t.CreatedByUserId == createdByUserIdFilter.Value);
            }
            else if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                query = query.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);
            }

            // Apply filters
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            if (assignedToUserId.HasValue)
            {
                query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
            }

            if (isOverdue.HasValue && isOverdue.Value)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled);
            }

            if (dueDateFrom.HasValue)
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= dueDateFrom.Value);
            }

            if (dueDateTo.HasValue)
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= dueDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => 
                    t.Title.Contains(searchTerm) || 
                    t.Description.Contains(searchTerm) ||
                    t.Category.Contains(searchTerm));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(); // Now db called

            var taskResponses = tasks.Select(MapToTaskResponse).ToList();

            return new TaskListResponse
            {
                Tasks = taskResponses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks for user {UserId}", userId);
            return new TaskListResponse();
        }
    }

    public async Task<TaskStatsResponse> GetTaskStatsAsync(int userId, string userRole)
    {
        try
        {
            var query = _context.Tasks.AsQueryable();

            // Apply authorization filter
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                query = query.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);
            }

            var now = DateTime.UtcNow;
            var weekFromNow = now.AddDays(7);

            var stats = new TaskStatsResponse
            {
                TotalTasks = await query.CountAsync(),
                PendingTasks = await query.CountAsync(t => t.Status == TaskStatus.Pending),
                InProgressTasks = await query.CountAsync(t => t.Status == TaskStatus.InProgress),
                CompletedTasks = await query.CountAsync(t => t.Status == TaskStatus.Completed),
                CancelledTasks = await query.CountAsync(t => t.Status == TaskStatus.Cancelled),
                OverdueTasks = await query.CountAsync(t => 
                    t.DueDate.HasValue && 
                    t.DueDate.Value < now && 
                    t.Status != TaskStatus.Completed && 
                    t.Status != TaskStatus.Cancelled),
                DueThisWeek = await query.CountAsync(t => 
                    t.DueDate.HasValue && 
                    t.DueDate.Value >= now && 
                    t.DueDate.Value <= weekFromNow &&
                    t.Status != TaskStatus.Completed && 
                    t.Status != TaskStatus.Cancelled)
            };

            _logger.LogInformation("Retrieved task stats for user {UserId}", userId);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task stats for user {UserId}", userId);
            return new TaskStatsResponse();
        }
    }

    public async Task<List<string>> GetCategoriesAsync(int userId, string userRole)
    {
        try
        {
            var query = _context.Tasks.AsQueryable();

            // Apply authorization filter
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                query = query.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);
            }

            var categories = await query
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for user {UserId}", userId);
            return new List<string>();
        }
    }

    // ==================== UPDATE ====================
    
    public async Task<TaskResponse?> UpdateTaskAsync(int taskId, UpdateTaskRequest request, int userId, string userRole)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return null;
            }

            // Check authorization
            if (userRole != "Admin" && userRole != "SuperAdmin" && task.CreatedByUserId != userId && task.AssignedToUserId != userId)
            {
                _logger.LogWarning("User {UserId} unauthorized to update task {TaskId}", userId, taskId);
                return null;
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Title))
                task.Title = request.Title;

            if (!string.IsNullOrWhiteSpace(request.Description))
                task.Description = request.Description;

            if (request.Status.HasValue)
                task.Status = request.Status.Value;

            if (request.Priority.HasValue)
                task.Priority = request.Priority.Value;

            if (!string.IsNullOrWhiteSpace(request.Category))
                task.Category = request.Category;

            if (request.DueDate.HasValue)
                task.DueDate = request.DueDate;

            if (request.AssignedToUserId.HasValue)
            {
                // Validate user exists
                var assignedUser = await _context.Users.FindAsync(request.AssignedToUserId.Value);
                if (assignedUser == null)
                {
                    _logger.LogWarning("Cannot assign task to non-existent user {UserId}", request.AssignedToUserId.Value);
                    return null;
                }
                task.AssignedToUserId = request.AssignedToUserId;
            }

            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} updated by user {UserId}", taskId, userId);

            return MapToTaskResponse(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId} by user {UserId}", taskId, userId);
            return null;
        }
    }

    public async Task<TaskResponse?> UpdateTaskStatusAsync(int taskId, TaskStatus status, int userId, string userRole)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return null;
            }

            // Check authorization
            if (userRole != "Admin" && userRole != "SuperAdmin" && task.CreatedByUserId != userId && task.AssignedToUserId != userId)
            {
                _logger.LogWarning("User {UserId} unauthorized to update task {TaskId} status", userId, taskId);
                return null;
            }

            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} status updated to {Status} by user {UserId}", taskId, status, userId);

            return MapToTaskResponse(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId} status by user {UserId}", taskId, userId);
            return null;
        }
    }

    public async Task<TaskResponse?> AssignTaskAsync(int taskId, int assignToUserId, int userId, string userRole)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return null;
            }

            // Check authorization - only creator or admin can assign tasks
            if (userRole != "Admin" && userRole != "SuperAdmin" && task.CreatedByUserId != userId)
            {
                _logger.LogWarning("User {UserId} unauthorized to assign task {TaskId}", userId, taskId);
                return null;
            }

            // Validate user exists
            var assignedUser = await _context.Users.FindAsync(assignToUserId);
            if (assignedUser == null)
            {
                _logger.LogWarning("Cannot assign task to non-existent user {UserId}", assignToUserId);
                return null;
            }

            task.AssignedToUserId = assignToUserId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} assigned to user {AssignedUserId} by user {UserId}", taskId, assignToUserId, userId);

            return MapToTaskResponse(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning task {TaskId} by user {UserId}", taskId, userId);
            return null;
        }
    }

    public async Task<TaskResponse?> UnassignTaskAsync(int taskId, int userId, string userRole)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return null;
            }

            // Check authorization - only creator, assigned user, or admin can unassign
            if (userRole != "Admin" && userRole != "SuperAdmin" && task.CreatedByUserId != userId && task.AssignedToUserId != userId)
            {
                _logger.LogWarning("User {UserId} unauthorized to unassign task {TaskId}", userId, taskId);
                return null;
            }

            task.AssignedToUserId = null;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} unassigned by user {UserId}", taskId, userId);

            return MapToTaskResponse(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning task {TaskId} by user {UserId}", taskId, userId);
            return null;
        }
    }

    // ==================== DELETE ====================
    
    public async Task<bool> DeleteTaskAsync(int taskId, int userId, string userRole)
    {
        try
        {
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return false;
            }

            // Check authorization - only creator or admin can delete
            if (userRole != "Admin" && userRole != "SuperAdmin" && task.CreatedByUserId != userId)
            {
                _logger.LogWarning("User {UserId} unauthorized to delete task {TaskId}", userId, taskId);
                return false;
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} deleted by user {UserId}", taskId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId} by user {UserId}", taskId, userId);
            return false;
        }
    }

    // ==================== BULK OPERATIONS ====================
    
    public async Task<bool> BulkDeleteTasksAsync(List<int> taskIds, int userId, string userRole)
    {
        try
        {
            var tasks = await _context.Tasks
                .Where(t => taskIds.Contains(t.Id))
                .ToListAsync();

            if (!tasks.Any())
            {
                _logger.LogWarning("No tasks found for bulk delete");
                return false;
            }

            // Filter by authorization
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                tasks = tasks.Where(t => t.CreatedByUserId == userId).ToList();
            }

            if (!tasks.Any())
            {
                _logger.LogWarning("User {UserId} unauthorized to delete any of the specified tasks", userId);
                return false;
            }

            _context.Tasks.RemoveRange(tasks);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk deleted {Count} tasks by user {UserId}", tasks.Count, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting tasks by user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> BulkUpdateStatusAsync(List<int> taskIds, TaskStatus status, int userId, string userRole)
    {
        try
        {
            var tasks = await _context.Tasks
                .Where(t => taskIds.Contains(t.Id))
                .ToListAsync();

            if (!tasks.Any())
            {
                _logger.LogWarning("No tasks found for bulk status update");
                return false;
            }

            // Filter by authorization
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                tasks = tasks.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId).ToList();
            }

            if (!tasks.Any())
            {
                _logger.LogWarning("User {UserId} unauthorized to update any of the specified tasks", userId);
                return false;
            }

            foreach (var task in tasks)
            {
                task.Status = status;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk updated status to {Status} for {Count} tasks by user {UserId}", status, tasks.Count, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating task status by user {UserId}", userId);
            return false;
        }
    }

    // ==================== HELPER METHODS ====================
    
    private static TaskResponse MapToTaskResponse(TaskItem task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            Category = task.Category,
            DueDate = task.DueDate,
            CreatedByUserId = task.CreatedByUserId,
            CreatedByUserName = task.CreatedByUser?.Name ?? string.Empty,
            AssignedToUserId = task.AssignedToUserId,
            AssignedToUserName = task.AssignedToUser?.Name,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
