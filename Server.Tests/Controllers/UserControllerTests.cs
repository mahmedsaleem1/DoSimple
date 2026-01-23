using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.DTOs;
using Server.Endpoints;
using Server.Services;
using System.Security.Claims;

namespace Server.Tests.Controllers;

/// <summary>
/// Unit tests for the UserController class.
/// These tests verify that the controller correctly handles HTTP requests
/// for user management operations (Admin only).
/// </summary>
public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UserController>> _loggerMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UserController>>();
        _controller = new UserController(_userServiceMock.Object, _loggerMock.Object);
        
        // Setup authenticated admin user context
        SetupAuthenticatedUser(userId: 1, role: "Admin");
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

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_ReturnsOkWithUserList()
    {
        // Arrange
        var response = new UserListResponse
        {
            Users = new List<UserResponse>
            {
                new UserResponse { Id = 1, Name = "User 1", Email = "user1@example.com" },
                new UserResponse { Id = 2, Name = "User 2", Email = "user2@example.com" }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _userServiceMock.Setup(x => x.GetUsersAsync(null, null, null, 1, 10))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetUsers(null, null, null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userList = Assert.IsType<UserListResponse>(okResult.Value);
        Assert.Equal(2, userList.Users.Count);
    }

    [Fact]
    public async Task GetUsers_WithFilters_PassesFiltersToService()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetUsersAsync("Admin", true, "test", 1, 10))
            .ReturnsAsync(new UserListResponse());

        // Act
        await _controller.GetUsers("Admin", true, "test", 1, 10);

        // Assert
        _userServiceMock.Verify(x => x.GetUsersAsync("Admin", true, "test", 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetUsers_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - Remove authentication
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetUsers(null, null, null, 1, 10);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetUsers_InvalidPageNumber_CorrectsToPaeOne()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetUsersAsync(null, null, null, 1, 10))
            .ReturnsAsync(new UserListResponse());

        // Act - Pass page number less than 1
        await _controller.GetUsers(null, null, null, -5, 10);

        // Assert - Should be corrected to page 1
        _userServiceMock.Verify(x => x.GetUsersAsync(null, null, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetUsers_PageSizeOverMax_CorrectsTtoMax()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetUsersAsync(null, null, null, 1, 100))
            .ReturnsAsync(new UserListResponse());

        // Act - Pass page size over max (100)
        await _controller.GetUsers(null, null, null, 1, 500);

        // Assert - Should be corrected to max 100
        _userServiceMock.Verify(x => x.GetUsersAsync(null, null, null, 1, 100), Times.Once);
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsOk()
    {
        // Arrange
        var response = new UserResponse
        {
            Id = 2,
            Name = "Test User",
            Email = "test@example.com"
        };

        _userServiceMock.Setup(x => x.GetUserByIdAsync(2))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetUserById(2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(2, user.Id);
    }

    [Fact]
    public async Task GetUserById_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetUserByIdAsync(999))
            .ReturnsAsync((UserResponse?)null);

        // Act
        var result = await _controller.GetUserById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        var response = new UserResponse
        {
            Id = 2,
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        _userServiceMock.Setup(x => x.UpdateUserAsync(2, request, 1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateUser(2, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal("Updated Name", user.Name);
    }

    [Fact]
    public async Task UpdateUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest { Name = "Updated" };

        _userServiceMock.Setup(x => x.UpdateUserAsync(999, request, 1))
            .ReturnsAsync((UserResponse?)null);

        // Act
        var result = await _controller.UpdateUser(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateUserRequest();
        _controller.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _controller.UpdateUser(2, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UpdateUserRole Tests

    [Fact]
    public async Task UpdateUserRole_ValidRole_ReturnsOk()
    {
        // Arrange
        var request = new UpdateUserRoleRequest { Role = "Admin" };
        var response = new UserResponse { Id = 2, Role = "Admin" };

        _userServiceMock.Setup(x => x.UpdateUserRoleAsync(2, "Admin", 1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateUserRole(2, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public async Task UpdateUserRole_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRoleRequest { Role = "Admin" };

        _userServiceMock.Setup(x => x.UpdateUserRoleAsync(999, "Admin", 1))
            .ReturnsAsync((UserResponse?)null);

        // Act
        var result = await _controller.UpdateUserRole(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region VerifyUserEmail Tests

    [Fact]
    public async Task VerifyUserEmail_ExistingUser_ReturnsOk()
    {
        // Arrange
        var response = new UserResponse { Id = 2, IsEmailVerified = true };

        _userServiceMock.Setup(x => x.VerifyUserEmailAsync(2, 1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.VerifyUserEmail(2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.True(user.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyUserEmail_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        _userServiceMock.Setup(x => x.VerifyUserEmailAsync(999, 1))
            .ReturnsAsync((UserResponse?)null);

        // Act
        var result = await _controller.VerifyUserEmail(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsOk()
    {
        // Arrange
        _userServiceMock.Setup(x => x.DeleteUserAsync(2, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteUser(2);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        _userServiceMock.Setup(x => x.DeleteUserAsync(999, 1))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteUser(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_DeletingSelf_ReturnsBadRequest()
    {
        // Arrange - Admin with id 1 trying to delete themselves

        // Act
        var result = await _controller.DeleteUser(1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetUserStats Tests

    [Fact]
    public async Task GetUserStats_ReturnsOkWithStats()
    {
        // Arrange
        var response = new UserStatsResponse
        {
            TotalUsers = 100,
            TotalAdmins = 5,
            VerifiedUsers = 80,
            UnverifiedUsers = 20,
            NewUsersThisMonth = 15
        };

        _userServiceMock.Setup(x => x.GetUserStatsAsync())
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetUserStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var stats = Assert.IsType<UserStatsResponse>(okResult.Value);
        Assert.Equal(100, stats.TotalUsers);
        Assert.Equal(5, stats.TotalAdmins);
    }

    #endregion
}
