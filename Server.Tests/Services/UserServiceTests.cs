using Server.Data;
using Server.DTOs;
using Server.Models;
using Server.Services;
using Server.Tests.Helpers;
using Server.Utills;

namespace Server.Tests.Services;

/// <summary>
/// Unit tests for the UserService class.
/// These tests verify user CRUD operations, filtering, statistics, and role management.
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        var logger = TestConfigurationFactory.CreateMockLogger<UserService>();

        _userService = new UserService(_context, logger);

        // Seed test data
        TestDbContextFactory.SeedUsers(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetUserById Tests

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUserResponse()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(userId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("testuser@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(userId: 9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_DoesNotReturnPassword()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(userId: 1);

        // Assert - UserResponse should not contain password
        Assert.NotNull(result);
        var responseType = result.GetType();
        var passwordProperty = responseType.GetProperty("Password");
        Assert.Null(passwordProperty);
    }

    #endregion

    #region GetUsers Tests

    [Fact]
    public async Task GetUsersAsync_NoFilters_ReturnsAllUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // We seeded 3 users
    }

    [Fact]
    public async Task GetUsersAsync_FilterByRole_ReturnsFilteredUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync(role: "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Users, u => Assert.Equal("Admin", u.Role));
    }

    [Fact]
    public async Task GetUsersAsync_FilterByEmailVerified_ReturnsFilteredUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync(isEmailVerified: true);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Users, u => Assert.True(u.IsEmailVerified));
    }

    [Fact]
    public async Task GetUsersAsync_FilterByUnverified_ReturnsUnverifiedUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync(isEmailVerified: false);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Users, u => Assert.False(u.IsEmailVerified));
    }

    [Fact]
    public async Task GetUsersAsync_SearchTerm_ReturnsMatchingUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync(searchTerm: "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Users.Any(u => u.Name.Contains("Admin") || u.Email.Contains("admin")));
    }

    [Fact]
    public async Task GetUsersAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange - Add more users
        for (int i = 0; i < 10; i++)
        {
            _context.Users.Add(new User
            {
                Name = $"Extra User {i}",
                Email = $"extra{i}@example.com",
                Password = PasswordHasher.HashPassword("password"),
                Role = "User",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUsersAsync(pageNumber: 2, pageSize: 5);

        // Assert
        Assert.Equal(5, result.PageSize);
        Assert.Equal(2, result.PageNumber);
        Assert.True(result.TotalCount > 5);
    }

    [Fact]
    public async Task GetUsersAsync_CalculatesTotalPagesCorrectly()
    {
        // Arrange - We have 3 seeded users
        // Act
        var result = await _userService.GetUsersAsync(pageSize: 2);

        // Assert
        Assert.Equal(2, result.TotalPages); // 3 users / 2 per page = 2 pages
    }

    #endregion

    #region GetUserStats Tests

    [Fact]
    public async Task GetUserStatsAsync_ReturnsCorrectStats()
    {
        // Act
        var result = await _userService.GetUserStatsAsync();

        // Assert
        Assert.Equal(3, result.TotalUsers);
        Assert.Equal(1, result.TotalAdmins); // Only "Admin" user from seed
        Assert.Equal(2, result.VerifiedUsers);
        Assert.Equal(1, result.UnverifiedUsers);
    }

    [Fact]
    public async Task GetUserStatsAsync_NewUsersThisMonth_CountsRecentUsers()
    {
        // Arrange - The seeded users have different creation dates
        // Add a user created this month
        _context.Users.Add(new User
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow // Created now, so this month
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserStatsAsync();

        // Assert
        Assert.True(result.NewUsersThisMonth >= 1);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUserAsync_ValidUpdate_ReturnsUpdatedUser()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest
        {
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var result = await _userService.UpdateUserAsync(userId: 1, updateRequest, adminUserId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("updated@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest { Name = "New Name" };

        // Act
        var result = await _userService.UpdateUserAsync(userId: 9999, updateRequest, adminUserId: 2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_DuplicateEmail_ReturnsNull()
    {
        // Arrange - Try to use email of user 2
        var updateRequest = new UpdateUserRequest
        {
            Email = "admin@example.com" // Already used by user 2
        };

        // Act
        var result = await _userService.UpdateUserAsync(userId: 1, updateRequest, adminUserId: 2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var originalUser = await _context.Users.FindAsync(1);
        var originalEmail = originalUser!.Email;

        var updateRequest = new UpdateUserRequest
        {
            Name = "Only Name Changed" // Only updating name
        };

        // Act
        var result = await _userService.UpdateUserAsync(userId: 1, updateRequest, adminUserId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Only Name Changed", result.Name);
        Assert.Equal(originalEmail, result.Email); // Email unchanged
    }

    [Fact]
    public async Task UpdateUserAsync_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest { Name = "Updated" };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _userService.UpdateUserAsync(userId: 1, updateRequest, adminUserId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt >= beforeUpdate);
    }

    #endregion

    #region UpdateUserRole Tests

    [Fact]
    public async Task UpdateUserRoleAsync_ValidRole_UpdatesRole()
    {
        // Act
        var result = await _userService.UpdateUserRoleAsync(userId: 1, role: "Admin", adminUserId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.UpdateUserRoleAsync(userId: 9999, role: "Admin", adminUserId: 2);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region VerifyUserEmail Tests

    [Fact]
    public async Task VerifyUserEmailAsync_UnverifiedUser_VerifiesEmail()
    {
        // Act - Verify the unverified user (id: 3)
        var result = await _userService.VerifyUserEmailAsync(userId: 3, adminUserId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyUserEmailAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.VerifyUserEmailAsync(userId: 9999, adminUserId: 2);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_ReturnsTrue()
    {
        // Arrange - Add a user to delete
        var userToDelete = new User
        {
            Name = "To Delete",
            Email = "todelete@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(userToDelete);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.DeleteUserAsync(userToDelete.Id, adminUserId: 2);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Users.FindAsync(userToDelete.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistentUser_ReturnsFalse()
    {
        // Act
        var result = await _userService.DeleteUserAsync(userId: 9999, adminUserId: 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUserPermanently()
    {
        // Arrange
        var userToDelete = new User
        {
            Name = "Permanent Delete",
            Email = "permanent@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(userToDelete);
        await _context.SaveChangesAsync();
        var userId = userToDelete.Id;
        var countBefore = _context.Users.Count();

        // Act
        await _userService.DeleteUserAsync(userId, adminUserId: 2);

        // Assert
        Assert.Equal(countBefore - 1, _context.Users.Count());
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetUsersAsync_EmptySearchTerm_ReturnsAllUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync(searchTerm: "");

        // Assert
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_WhitespaceSearchTerm_ReturnsAllUsers()
    {
        // Act
        var result = await _userService.GetUsersAsync(searchTerm: "   ");

        // Assert
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task UpdateUserAsync_EmptyUpdateRequest_ReturnsUnchangedUser()
    {
        // Arrange
        var originalUser = await _userService.GetUserByIdAsync(1);
        var updateRequest = new UpdateUserRequest(); // All null/empty

        // Act
        var result = await _userService.UpdateUserAsync(userId: 1, updateRequest, adminUserId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalUser!.Name, result.Name);
        Assert.Equal(originalUser.Email, result.Email);
    }

    #endregion
}
