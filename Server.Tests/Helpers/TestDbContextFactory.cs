using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using TaskStatus = Server.Models.TaskStatus;

namespace Server.Tests.Helpers;

/// <summary>
/// Factory class to create in-memory database contexts for testing.
/// This allows tests to run against a real database context without needing
/// an actual SQL Server connection.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory AppDbContext for testing purposes.
    /// Each call creates a unique database instance to ensure test isolation.
    /// </summary>
    /// <param name="databaseName">Optional database name. If not provided, a unique GUID is used.</param>
    /// <returns>An AppDbContext instance using in-memory storage</returns>
    public static AppDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Seeds the database with sample users for testing
    /// </summary>
    public static void SeedUsers(AppDbContext context)
    {
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Name = "Test User",
                Email = "testuser@example.com",
                Password = Server.Utills.PasswordHasher.HashPassword("password123"),
                Role = "User",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                Id = 2,
                Name = "Test Admin",
                Email = "admin@example.com",
                Password = Server.Utills.PasswordHasher.HashPassword("adminpass123"),
                Role = "Admin",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new User
            {
                Id = 3,
                Name = "Unverified User",
                Email = "unverified@example.com",
                Password = Server.Utills.PasswordHasher.HashPassword("unverified123"),
                Role = "User",
                IsEmailVerified = false,
                EmailVerificationToken = "test-verification-token",
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();
    }

    /// <summary>
    /// Seeds the database with sample tasks for testing
    /// </summary>
    public static void SeedTasks(AppDbContext context)
    {
        // Ensure users exist first
        if (!context.Users.Any())
        {
            SeedUsers(context);
        }

        var tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = 1,
                Title = "Test Task 1",
                Description = "This is a test task",
                Status = TaskStatus.Pending,
                Priority = TaskPriority.High,
                Category = "Development",
                CreatedByUserId = 1,
                AssignedToUserId = 2,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new TaskItem
            {
                Id = 2,
                Title = "Test Task 2",
                Description = "Another test task",
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                Category = "Testing",
                CreatedByUserId = 2,
                AssignedToUserId = 1,
                DueDate = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new TaskItem
            {
                Id = 3,
                Title = "Completed Task",
                Description = "A completed task",
                Status = TaskStatus.Completed,
                Priority = TaskPriority.Low,
                Category = "Documentation",
                CreatedByUserId = 1,
                AssignedToUserId = null,
                DueDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TaskItem
            {
                Id = 4,
                Title = "Overdue Task",
                Description = "This task is overdue",
                Status = TaskStatus.Pending,
                Priority = TaskPriority.Critical,
                Category = "Urgent",
                CreatedByUserId = 2,
                AssignedToUserId = 1,
                DueDate = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            }
        };

        context.Tasks.AddRange(tasks);
        context.SaveChanges();
    }
}
