using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.DTOs;
using Server.Endpoints;
using Server.Models;
using Server.Services;
using System.Security.Claims;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.Tests.Controllers;

/// <summary>
/// Unit tests for the TaskController class.
/// These tests verify that the controller correctly handles HTTP requests
/// and returns appropriate responses based on service results.
/// </summary>
public class TaskControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly Mock<ILogger<TaskController>> _loggerMock;
    private readonly TaskController _controller;

    public TaskControllerTests()
    {
        _taskServiceMock = new Mock<ITaskService>();
        _loggerMock = new Mock<ILogger<TaskController>>();
        _controller = new TaskController(_taskServiceMock.Object, _loggerMock.Object);
        
        // Setup authenticated user context
        SetupAuthenticatedUser(userId: 1, role: "User");
    }

    private void SetupAuthenticatedUser(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region CreateTask Tests

    [Fact]
    public async Task CreateTask_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Description",
            Priority = TaskPriority.High,
            Category = "Development"
        };

        var response = new TaskResponse
        {
            Id = 1,
            Title = request.Title,
            Description = request.Description,
            Status = "Pending",
            Priority = "High",
            Category = request.Category
        };

        _taskServiceMock.Setup(x => x.CreateTaskAsync(request, 1, null))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateTask(request, null);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedTask = Assert.IsType<TaskResponse>(createdResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
    }

    [Fact]
    public async Task CreateTask_ServiceFails_ReturnsBadRequest()
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

        _taskServiceMock.Setup(x => x.CreateTaskAsync(request, 1, null))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.CreateTask(request, null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateTask_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - Remove authentication
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new CreateTaskRequest
        {
            Title = "Task",
            Description = "Description",
            Priority = TaskPriority.Low,
            Category = "Test"
        };

        // Act
        var result = await _controller.CreateTask(request, null);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateTask_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaskRequest();
        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateTask(request, null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetTasks Tests

    [Fact]
    public async Task GetTasks_ReturnsOkWithTaskList()
    {
        // Arrange
        var response = new TaskListResponse
        {
            Tasks = new List<TaskResponse>
            {
                new TaskResponse { Id = 1, Title = "Task 1" },
                new TaskResponse { Id = 2, Title = "Task 2" }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _taskServiceMock.Setup(x => x.GetTasksAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<TaskStatus?>(),
            It.IsAny<TaskPriority?>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<bool?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int?>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetTasks(null, null, null, null, null, null, null, null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var taskList = Assert.IsType<TaskListResponse>(okResult.Value);
        Assert.Equal(2, taskList.Tasks.Count);
    }

    [Fact]
    public async Task GetTasks_WithFilters_PassesFiltersToService()
    {
        // Arrange
        _taskServiceMock.Setup(x => x.GetTasksAsync(
            1, "User", TaskStatus.Pending, TaskPriority.High,
            "Development", null, null, null, null, null, 1, 10, null))
            .ReturnsAsync(new TaskListResponse());

        // Act
        await _controller.GetTasks(TaskStatus.Pending, TaskPriority.High, "Development", null, null, null, null, null, 1, 10);

        // Assert
        _taskServiceMock.Verify(x => x.GetTasksAsync(
            1, "User", TaskStatus.Pending, TaskPriority.High,
            "Development", null, null, null, null, null, 1, 10, null), Times.Once);
    }

    #endregion

    #region GetTaskById Tests

    [Fact]
    public async Task GetTaskById_ExistingTask_ReturnsOk()
    {
        // Arrange
        var response = new TaskResponse { Id = 1, Title = "Task 1" };

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(1, 1, "User"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetTaskById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal(1, task.Id);
    }

    [Fact]
    public async Task GetTaskById_NonExistentTask_ReturnsNotFound()
    {
        // Arrange
        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(999, 1, "User"))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.GetTaskById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async Task UpdateTask_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Priority = TaskPriority.Critical
        };

        var response = new TaskResponse
        {
            Id = 1,
            Title = "Updated Title",
            Priority = "Critical"
        };

        _taskServiceMock.Setup(x => x.UpdateTaskAsync(1, request, 1, "User", null))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateTask(1, request, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal("Updated Title", task.Title);
    }

    [Fact]
    public async Task UpdateTask_NonExistentTask_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateTaskRequest { Title = "Updated" };

        _taskServiceMock.Setup(x => x.UpdateTaskAsync(999, request, 1, "User", null))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.UpdateTask(999, request, null);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region UpdateTaskStatus Tests

    [Fact]
    public async Task UpdateTaskStatus_ValidStatus_ReturnsOk()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest { Status = TaskStatus.Completed };
        var response = new TaskResponse { Id = 1, Status = "Completed" };

        _taskServiceMock.Setup(x => x.UpdateTaskStatusAsync(1, TaskStatus.Completed, 1, "User"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateTaskStatus(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal("Completed", task.Status);
    }

    #endregion

    #region DeleteTask Tests

    [Fact]
    public async Task DeleteTask_ExistingTask_ReturnsOk()
    {
        // Arrange
        _taskServiceMock.Setup(x => x.DeleteTaskAsync(1, 1, "User"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteTask(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteTask_NonExistentTask_ReturnsNotFound()
    {
        // Arrange
        _taskServiceMock.Setup(x => x.DeleteTaskAsync(999, 1, "User"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTask(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region AssignTask Tests

    [Fact]
    public async Task AssignTask_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new AssignTaskRequest { UserId = 2 };
        var response = new TaskResponse { Id = 1, AssignedToUserId = 2 };

        _taskServiceMock.Setup(x => x.AssignTaskAsync(1, 2, 1, "User"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.AssignTask(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal(2, task.AssignedToUserId);
    }

    #endregion

    #region GetTaskStats Tests

    [Fact]
    public async Task GetTaskStats_ReturnsOkWithStats()
    {
        // Arrange
        var response = new TaskStatsResponse
        {
            TotalTasks = 10,
            PendingTasks = 5,
            InProgressTasks = 3,
            CompletedTasks = 2
        };

        _taskServiceMock.Setup(x => x.GetTaskStatsAsync(1, "User"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetTaskStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var stats = Assert.IsType<TaskStatsResponse>(okResult.Value);
        Assert.Equal(10, stats.TotalTasks);
    }

    #endregion

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_ReturnsOkWithCategories()
    {
        // Arrange
        var categories = new List<string> { "Development", "Testing", "Documentation" };

        _taskServiceMock.Setup(x => x.GetCategoriesAsync(1, "User"))
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCategories = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Equal(3, returnedCategories.Count);
    }

    #endregion

    #region BulkDelete Tests

    [Fact]
    public async Task BulkDelete_ValidRequest_ReturnsOk()
    {
        // Arrange
        var taskIds = new List<int> { 1, 2, 3 };

        _taskServiceMock.Setup(x => x.BulkDeleteTasksAsync(taskIds, 1, "User"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.BulkDeleteTasks(taskIds);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region BulkUpdateStatus Tests

    [Fact]
    public async Task BulkUpdateStatus_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new Server.Endpoints.BulkUpdateStatusRequest
        {
            TaskIds = new List<int> { 1, 2 },
            Status = TaskStatus.Completed
        };

        _taskServiceMock.Setup(x => x.BulkUpdateStatusAsync(
            It.IsAny<List<int>>(), TaskStatus.Completed, 1, "User"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.BulkUpdateStatus(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion
}
