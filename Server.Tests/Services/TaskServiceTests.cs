using Moq;
using Server.Data;
using Server.DTOs;
using Server.Models;
using Server.Services;
using Server.Tests.Helpers;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.Tests.Services;

/// <summary>
/// Unit tests for the TaskService class.
/// These tests verify task CRUD operations, filtering, statistics, and authorization.
/// </summary>
public class TaskServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _cloudinaryServiceMock = MockServiceFactory.CreateCloudinaryServiceMock();
        var logger = TestConfigurationFactory.CreateMockLogger<TaskService>();

        _taskService = new TaskService(_context, logger, _cloudinaryServiceMock.Object);
        
        // Seed test data
        TestDbContextFactory.SeedUsers(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Create Task Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidRequest_ReturnsTaskResponse()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Task description",
            Priority = TaskPriority.High,
            Category = "Development",
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request, createdByUserId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Task", result.Title);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("High", result.Priority);
        Assert.Equal("Development", result.Category);
    }

    [Fact]
    public async Task CreateTaskAsync_WithAssignedUser_SetsAssignment()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Assigned Task",
            Description = "Task assigned to someone",
            Priority = TaskPriority.Medium,
            Category = "Testing",
            AssignedToUserId = 2 // Assign to user ID 2
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request, createdByUserId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.AssignedToUserId);
        Assert.NotNull(result.AssignedToUserName);
    }

    [Fact]
    public async Task CreateTaskAsync_WithNonExistentAssignedUser_ReturnsNull()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Task",
            Description = "Description",
            Priority = TaskPriority.Low,
            Category = "Test",
            AssignedToUserId = 9999 // Non-existent user
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request, createdByUserId: 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTaskAsync_DefaultStatusIsPending()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Description",
            Priority = TaskPriority.Medium,
            Category = "General"
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request, createdByUserId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
    }

    #endregion

    #region Get Task By Id Tests

    [Fact]
    public async Task GetTaskByIdAsync_AsCreator_ReturnsTask()
    {
        // Arrange - Create a task
        var task = new TaskItem
        {
            Title = "Test Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTaskByIdAsync(task.Id, userId: 1, userRole: "User");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(task.Title, result.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AsAdmin_CanAccessAnyTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Other User's Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 3, // Created by another user
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act - Admin (user 2) tries to access
        var result = await _taskService.GetTaskByIdAsync(task.Id, userId: 2, userRole: "Admin");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AsNonOwner_ReturnsNull()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Other User's Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 2,
            AssignedToUserId = 3, // Not assigned to user 1
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act - User 1 tries to access (not creator or assignee)
        var result = await _taskService.GetTaskByIdAsync(task.Id, userId: 1, userRole: "User");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistentTask_ReturnsNull()
    {
        // Act
        var result = await _taskService.GetTaskByIdAsync(taskId: 9999, userId: 1, userRole: "Admin");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Get Tasks List Tests

    [Fact]
    public async Task GetTasksAsync_ReturnsPagedResults()
    {
        // Arrange - Create multiple tasks
        for (int i = 0; i < 15; i++)
        {
            _context.Tasks.Add(new TaskItem
            {
                Title = $"Task {i}",
                Description = $"Description {i}",
                Status = TaskStatus.Pending,
                Priority = TaskPriority.Medium,
                Category = "Test",
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksAsync(
            userId: 1,
            userRole: "User",
            pageNumber: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(10, result.Tasks.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetTasksAsync_FilterByStatus_ReturnsFilteredTasks()
    {
        // Arrange
        _context.Tasks.Add(new TaskItem
        {
            Title = "Pending Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _context.Tasks.Add(new TaskItem
        {
            Title = "Completed Task",
            Description = "Description",
            Status = TaskStatus.Completed,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksAsync(
            userId: 1,
            userRole: "User",
            status: TaskStatus.Pending);

        // Assert
        Assert.All(result.Tasks, t => Assert.Equal("Pending", t.Status));
    }

    [Fact]
    public async Task GetTasksAsync_FilterByPriority_ReturnsFilteredTasks()
    {
        // Arrange
        _context.Tasks.Add(new TaskItem
        {
            Title = "High Priority",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.High,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _context.Tasks.Add(new TaskItem
        {
            Title = "Low Priority",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Low,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksAsync(
            userId: 1,
            userRole: "User",
            priority: TaskPriority.High);

        // Assert
        Assert.All(result.Tasks, t => Assert.Equal("High", t.Priority));
    }

    [Fact]
    public async Task GetTasksAsync_FilterByCategory_ReturnsFilteredTasks()
    {
        // Arrange
        _context.Tasks.Add(new TaskItem
        {
            Title = "Dev Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Development",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _context.Tasks.Add(new TaskItem
        {
            Title = "Test Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Testing",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksAsync(
            userId: 1,
            userRole: "User",
            category: "Development");

        // Assert
        Assert.All(result.Tasks, t => Assert.Equal("Development", t.Category));
    }

    [Fact]
    public async Task GetTasksAsync_SearchTerm_ReturnsMatchingTasks()
    {
        // Arrange
        _context.Tasks.Add(new TaskItem
        {
            Title = "Important Meeting",
            Description = "Weekly sync",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "General",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _context.Tasks.Add(new TaskItem
        {
            Title = "Code Review",
            Description = "Review PR",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Development",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTasksAsync(
            userId: 1,
            userRole: "User",
            searchTerm: "Meeting");

        // Assert
        Assert.Single(result.Tasks);
        Assert.Contains("Meeting", result.Tasks[0].Title);
    }

    #endregion

    #region Update Task Tests

    [Fact]
    public async Task UpdateTaskAsync_AsOwner_UpdatesTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Original Title",
            Description = "Original Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Low,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Priority = TaskPriority.High
        };

        // Act
        var result = await _taskService.UpdateTaskAsync(task.Id, updateRequest, userId: 1, userRole: "User");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("High", result.Priority);
    }

    [Fact]
    public async Task UpdateTaskAsync_AsNonOwner_ReturnsNull()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Original Title",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 2, // Created by user 2
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateTaskRequest { Title = "Hacked Title" };

        // Act - User 1 tries to update
        var result = await _taskService.UpdateTaskAsync(task.Id, updateRequest, userId: 1, userRole: "User");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Update Task Status Tests

    [Fact]
    public async Task UpdateTaskStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.UpdateTaskStatusAsync(
            task.Id,
            TaskStatus.InProgress,
            userId: 1,
            userRole: "User");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("InProgress", result.Status);
    }

    #endregion

    #region Delete Task Tests

    [Fact]
    public async Task DeleteTaskAsync_AsOwner_DeletesTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Task to Delete",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        var taskId = task.Id;

        // Act
        var result = await _taskService.DeleteTaskAsync(taskId, userId: 1, userRole: "User");

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Tasks.FindAsync(taskId));
    }

    [Fact]
    public async Task DeleteTaskAsync_AsAdmin_CanDeleteAnyTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Task to Delete",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 3, // Different user
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.DeleteTaskAsync(task.Id, userId: 2, userRole: "Admin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistentTask_ReturnsFalse()
    {
        // Act
        var result = await _taskService.DeleteTaskAsync(taskId: 9999, userId: 1, userRole: "Admin");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Task Stats Tests

    [Fact]
    public async Task GetTaskStatsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        _context.Tasks.AddRange(new List<TaskItem>
        {
            new TaskItem { Title = "T1", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T2", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T3", Description = "D", Status = TaskStatus.InProgress, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T4", Description = "D", Status = TaskStatus.Completed, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTaskStatsAsync(userId: 1, userRole: "User");

        // Assert
        Assert.Equal(4, result.TotalTasks);
        Assert.Equal(2, result.PendingTasks);
        Assert.Equal(1, result.InProgressTasks);
        Assert.Equal(1, result.CompletedTasks);
    }

    #endregion

    #region Assign Task Tests

    [Fact]
    public async Task AssignTaskAsync_ValidUser_AssignsTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Task to Assign",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.AssignTaskAsync(
            task.Id,
            assignToUserId: 2,
            userId: 1,
            userRole: "User");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.AssignedToUserId);
    }

    [Fact]
    public async Task UnassignTaskAsync_RemovesAssignment()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Assigned Task",
            Description = "Description",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            Category = "Test",
            CreatedByUserId = 1,
            AssignedToUserId = 2,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.UnassignTaskAsync(task.Id, userId: 1, userRole: "User");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AssignedToUserId);
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkDeleteTasksAsync_DeletesMultipleTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem { Title = "T1", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T2", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T3", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow }
        };
        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();

        // Act
        var result = await _taskService.BulkDeleteTasksAsync(taskIds, userId: 1, userRole: "User");

        // Assert
        Assert.True(result);
        Assert.Equal(0, _context.Tasks.Count());
    }

    [Fact]
    public async Task BulkUpdateStatusAsync_UpdatesMultipleTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem { Title = "T1", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T2", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "C", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow }
        };
        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();

        // Act
        var result = await _taskService.BulkUpdateStatusAsync(
            taskIds,
            TaskStatus.Completed,
            userId: 1,
            userRole: "User");

        // Assert
        Assert.True(result);
        Assert.All(_context.Tasks, t => Assert.Equal(TaskStatus.Completed, t.Status));
    }

    #endregion

    #region Get Categories Tests

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        _context.Tasks.AddRange(new List<TaskItem>
        {
            new TaskItem { Title = "T1", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "Development", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T2", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "Testing", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T3", Description = "D", Status = TaskStatus.Pending, Priority = TaskPriority.Low, Category = "Development", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetCategoriesAsync(userId: 1, userRole: "User");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Development", result);
        Assert.Contains("Testing", result);
    }

    #endregion
}
