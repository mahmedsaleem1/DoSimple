using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== READ ====================

    public async Task<UserResponse?> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            return MapToUserResponse(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return null;
        }
    }

    public async Task<UserListResponse> GetUsersAsync(
        string? role = null,
        bool? isEmailVerified = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (isEmailVerified.HasValue)
            {
                query = query.Where(u => u.IsEmailVerified == isEmailVerified.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u =>
                    u.Name.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userResponses = users.Select(MapToUserResponse).ToList();

            return new UserListResponse
            {
                Users = userResponses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return new UserListResponse();
        }
    }

    public async Task<UserStatsResponse> GetUserStatsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var stats = new UserStatsResponse
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin" || u.Role == "SuperAdmin"),
                VerifiedUsers = await _context.Users.CountAsync(u => u.IsEmailVerified),
                UnverifiedUsers = await _context.Users.CountAsync(u => !u.IsEmailVerified),
                NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth)
            };

            _logger.LogInformation("Retrieved user stats");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user stats");
            return new UserStatsResponse();
        }
    }

    // ==================== UPDATE ====================

    public async Task<UserResponse?> UpdateUserAsync(int userId, UpdateUserRequest request, int adminUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if email is already in use by another user
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == request.Email && u.Id != userId);

                if (emailExists)
                {
                    _logger.LogWarning("Email {Email} already in use", request.Email);
                    return null;
                }

                user.Email = request.Email;
            }

            if (request.IsEmailVerified.HasValue)
            {
                user.IsEmailVerified = request.IsEmailVerified.Value;
                if (user.IsEmailVerified)
                {
                    user.EmailVerificationToken = null;
                    user.EmailVerificationTokenExpiry = null;
                }
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated by admin {AdminId}", userId, adminUserId);

            return MapToUserResponse(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} by admin {AdminId}", userId, adminUserId);
            return null;
        }
    }

    public async Task<UserResponse?> UpdateUserRoleAsync(int userId, string role, int adminUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} role updated to {Role} by admin {AdminId}", userId, role, adminUserId);

            return MapToUserResponse(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} role by admin {AdminId}", userId, adminUserId);
            return null;
        }
    }

    public async Task<UserResponse?> VerifyUserEmailAsync(int userId, int adminUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} email verified by admin {AdminId}", userId, adminUserId);

            return MapToUserResponse(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying user {UserId} email by admin {AdminId}", userId, adminUserId);
            return null;
        }
    }

    // ==================== DELETE ====================

    public async Task<bool> DeleteUserAsync(int userId, int adminUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // Delete all tasks created by this user
            var userTasks = await _context.Tasks
                .Where(t => t.CreatedByUserId == userId)
                .ToListAsync();

            _context.Tasks.RemoveRange(userTasks);

            // Unassign tasks assigned to this user
            var assignedTasks = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId)
                .ToListAsync();

            foreach (var task in assignedTasks)
            {
                task.AssignedToUserId = null;
                task.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} and their tasks deleted by admin {AdminId}", userId, adminUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} by admin {AdminId}", userId, adminUserId);
            return false;
        }
    }

    // ==================== HELPER METHODS ====================

    private static UserResponse MapToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
