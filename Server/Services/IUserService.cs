using Server.DTOs;

namespace Server.Services;

public interface IUserService
{
    // Read
    Task<UserResponse?> GetUserByIdAsync(int userId);
    Task<UserListResponse> GetUsersAsync(
        string? role = null,
        bool? isEmailVerified = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<UserStatsResponse> GetUserStatsAsync();
    
    // Update
    Task<UserResponse?> UpdateUserAsync(int userId, UpdateUserRequest request, int adminUserId);
    Task<UserResponse?> UpdateUserRoleAsync(int userId, string role, int adminUserId);
    Task<UserResponse?> VerifyUserEmailAsync(int userId, int adminUserId);
    
    // Delete
    Task<bool> DeleteUserAsync(int userId, int adminUserId);
}
