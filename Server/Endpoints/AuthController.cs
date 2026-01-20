using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Services;

namespace Server.Endpoints;

[ApiController]   //Enables API behavior
[Route("api/[controller]")]  // base route + /auth {AuthController}
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")] // api/auth/register
    [AllowAnonymous]  // No need of Token here
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request);

        if (result == null)
        {
            return BadRequest(new { message = "User registration failed. Email may already be in use." });
        }

        _logger.LogInformation("User registered successfully: {Email}", request.Email);

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")] // api/auth/login
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);

        return Ok(result);
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.ForgotPasswordAsync(request);

        if (!result)
        {
            return BadRequest(new { message = "Failed to process password reset request." });
        }

        return Ok(new { message = "If the email exists, a password reset link has been sent." });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.ResetPasswordAsync(request);

        if (!result)
        {
            return BadRequest(new { message = "Password reset failed. Invalid or expired token." });
        }

        return Ok(new { message = "Password has been reset successfully." });
    }

    /// <summary>
    /// Verify email address with token
    /// </summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        var result = await _authService.VerifyEmailAsync(token);

        if (!result)
        {
            return BadRequest(new { message = "Email verification failed. Invalid or expired token." });
        }

        return Ok(new { message = "Email verified successfully! You can now close this window." });
    }

    /// <summary>
    /// Test endpoint to verify authentication is working
    /// </summary>
    [HttpGet("verify")]
    [Authorize]
    public IActionResult Verify()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "Token is valid",
            userId,
            email,
            name,
            role
        });
    }
}
