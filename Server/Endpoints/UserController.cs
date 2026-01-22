using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Services;

namespace Server.Endpoints;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")] // Only admins can access these endpoints
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with pagination
    /// Query parameters:
    /// - role: Filter by role (User, Admin, SuperAdmin)
    /// - isEmailVerified: Filter by email verification status (true/false)
    /// - searchTerm: Search in name and email
    /// - pageNumber: Page number (default: 1)
    /// - pageSize: Items per page (default: 10, max: 100)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role,
        [FromQuery] bool? isEmailVerified,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var adminUserId = GetUserId();

        if (adminUserId == 0)
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _userService.GetUsersAsync(
            role,
            isEmailVerified,
            searchTerm,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);

        if (result == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update user information (name, email, email verification status)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminUserId = GetUserId();

        var result = await _userService.UpdateUserAsync(id, request, adminUserId);

        if (result == null)
        {
            return NotFound(new { message = "User not found or email already in use" });
        }

        _logger.LogInformation("User {UserId} updated by admin {AdminId}", id, adminUserId);

        return Ok(result);
    }

    /// <summary>
    /// Update user role
    /// </summary>
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminUserId = GetUserId();

        var result = await _userService.UpdateUserRoleAsync(id, request.Role, adminUserId);

        if (result == null)
        {
            return NotFound(new { message = "User not found" });
        }

        _logger.LogInformation("User {UserId} role updated to {Role} by admin {AdminId}", id, request.Role, adminUserId);

        return Ok(result);
    }

    /// <summary>
    /// Verify user's email (admin can manually verify)
    /// </summary>
    [HttpPatch("{id}/verify-email")]
    public async Task<IActionResult> VerifyUserEmail(int id)
    {
        var adminUserId = GetUserId();

        var result = await _userService.VerifyUserEmailAsync(id, adminUserId);

        if (result == null)
        {
            return NotFound(new { message = "User not found" });
        }

        _logger.LogInformation("User {UserId} email verified by admin {AdminId}", id, adminUserId);

        return Ok(result);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var adminUserId = GetUserId();

        // Prevent admin from deleting themselves
        if (adminUserId == id)
        {
            return BadRequest(new { message = "You cannot delete your own account" });
        }

        var result = await _userService.DeleteUserAsync(id, adminUserId);

        if (!result)
        {
            return NotFound(new { message = "User not found" });
        }

        _logger.LogInformation("User {UserId} deleted by admin {AdminId}", id, adminUserId);

        return Ok(new { message = "User deleted successfully" });
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats()
    {
        var result = await _userService.GetUserStatsAsync();

        return Ok(result);
    }

    // ==================== HELPER METHODS ====================

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
