using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Models;
using Server.Services;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.Endpoints;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskController> _logger;

    public TaskController(ITaskService taskService, ILogger<TaskController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    // ==================== CREATE ====================

    /// <summary>
    /// Create a new task
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.CreateTaskAsync(request, userId);

        if (result == null)
        {
            return BadRequest(new { message = "Failed to create task. Assigned user may not exist." });
        }

        _logger.LogInformation("Task {TaskId} created by user {UserId}", result.Id, userId);

        return CreatedAtAction(nameof(GetTaskById), new { id = result.Id }, result);
    }

    // ==================== READ ====================

    /// <summary>
    /// Get all tasks with optional filters and pagination
    /// Query parameters:
    /// - status: Filter by task status (Pending=0, InProgress=1, Completed=2, Cancelled=3)
    /// - priority: Filter by priority (Low=0, Medium=1, High=2, Critical=3)
    /// - category: Filter by category name
    /// - assignedToUserId: Filter by assigned user ID
    /// - isOverdue: Filter overdue tasks (true/false)
    /// - dueDateFrom: Filter tasks due from this date
    /// - dueDateTo: Filter tasks due until this date
    /// - searchTerm: Search in title, description, and category
    /// - pageNumber: Page number (default: 1)
    /// - pageSize: Items per page (default: 10)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] TaskStatus? status,
        [FromQuery] TaskPriority? priority,
        [FromQuery] string? category,
        [FromQuery] int? assignedToUserId,
        [FromQuery] bool? isOverdue,
        [FromQuery] DateTime? dueDateFrom,
        [FromQuery] DateTime? dueDateTo,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size

        var result = await _taskService.GetTasksAsync(
            userId,
            userRole,
            status,
            priority,
            category,
            assignedToUserId,
            isOverdue,
            dueDateFrom,
            dueDateTo,
            searchTerm,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get task statistics for dashboard
    /// Returns counts for total, pending, in-progress, completed, cancelled, overdue, and due this week
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetTaskStats()
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.GetTaskStatsAsync(userId, userRole);

        return Ok(result);
    }

    /// <summary>
    /// Get all distinct categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.GetCategoriesAsync(userId, userRole);

        return Ok(result);
    }

    /// <summary>
    /// Get tasks assigned to the current user
    /// </summary>
    [HttpGet("my-assigned")]
    public async Task<IActionResult> GetMyAssignedTasks(
        [FromQuery] TaskStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.GetTasksAsync(
            userId,
            userRole,
            status,
            assignedToUserId: userId,
            pageNumber: pageNumber,
            pageSize: pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get tasks created by the current user
    /// </summary>
    [HttpGet("my-created")]
    public async Task<IActionResult> GetMyCreatedTasks(
        [FromQuery] TaskStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.GetTasksAsync(
            userId,
            userRole,
            status,
            pageNumber: pageNumber,
            pageSize: pageSize,
            createdByUserIdFilter: userId);

        return Ok(result);
    }

    /// <summary>
    /// Get overdue tasks
    /// </summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueTasks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.GetTasksAsync(
            userId,
            userRole,
            isOverdue: true,
            pageNumber: pageNumber,
            pageSize: pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(int id)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.GetTaskByIdAsync(id, userId, userRole);

        if (result == null)
        {
            return NotFound(new { message = "Task not found or you don't have access" });
        }

        return Ok(result);
    }

    // ==================== UPDATE ====================

    /// <summary>
    /// Update a task (partial update)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.UpdateTaskAsync(id, request, userId, userRole);

        if (result == null)
        {
            return NotFound(new { message = "Task not found or you don't have permission to update it" });
        }

        _logger.LogInformation("Task {TaskId} updated by user {UserId}", id, userId);

        return Ok(result);
    }

    /// <summary>
    /// Update only the task status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.UpdateTaskStatusAsync(id, request.Status, userId, userRole);

        if (result == null)
        {
            return NotFound(new { message = "Task not found or you don't have permission to update it" });
        }

        _logger.LogInformation("Task {TaskId} status updated to {Status} by user {UserId}", id, request.Status, userId);

        return Ok(result);
    }

    /// <summary>
    /// Assign a task to a user
    /// </summary>
    [HttpPut("{id}/assign")]
    public async Task<IActionResult> AssignTask(int id, [FromBody] AssignTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.AssignTaskAsync(id, request.UserId, userId, userRole);

        if (result == null)
        {
            return NotFound(new { message = "Task not found, user doesn't exist, or you don't have permission" });
        }

        _logger.LogInformation("Task {TaskId} assigned to user {AssignedUserId} by user {UserId}", id, request.UserId, userId);

        return Ok(result);
    }

    /// <summary>
    /// Unassign a task (remove assigned user)
    /// </summary>
    [HttpPut("{id}/unassign")]
    public async Task<IActionResult> UnassignTask(int id)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.UnassignTaskAsync(id, userId, userRole);

        if (result == null)
        {
            return NotFound(new { message = "Task not found or you don't have permission" });
        }

        _logger.LogInformation("Task {TaskId} unassigned by user {UserId}", id, userId);

        return Ok(result);
    }

    // ==================== BULK OPERATIONS ====================

    /// <summary>
    /// Bulk delete tasks
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteTasks([FromBody] List<int> taskIds)
    {
        if (taskIds == null || !taskIds.Any())
        {
            return BadRequest(new { message = "No task IDs provided" });
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.BulkDeleteTasksAsync(taskIds, userId, userRole);

        if (!result)
        {
            return BadRequest(new { message = "Failed to delete tasks or you don't have permission" });
        }

        _logger.LogInformation("Bulk delete of tasks by user {UserId}", userId);

        return Ok(new { message = "Tasks deleted successfully" });
    }

    /// <summary>
    /// Bulk update task status
    /// </summary>
    [HttpPost("bulk-update-status")]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return BadRequest(new { message = "No task IDs provided" });
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.BulkUpdateStatusAsync(request.TaskIds, request.Status, userId, userRole);

        if (!result)
        {
            return BadRequest(new { message = "Failed to update tasks or you don't have permission" });
        }

        _logger.LogInformation("Bulk status update to {Status} by user {UserId}", request.Status, userId);

        return Ok(new { message = "Tasks status updated successfully" });
    }

    // ==================== DELETE ====================

    /// <summary>
    /// Delete a task
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.DeleteTaskAsync(id, userId, userRole);

        if (!result)
        {
            return NotFound(new { message = "Task not found or you don't have permission to delete it" });
        }

        _logger.LogInformation("Task {TaskId} deleted by user {UserId}", id, userId);

        return Ok(new { message = "Task deleted successfully" });
    }

    // ==================== HELPER METHODS ====================

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
    }
}

// Additional DTO for bulk update status
public class BulkUpdateStatusRequest
{
    public List<int> TaskIds { get; set; } = new();
    public TaskStatus Status { get; set; }
}
