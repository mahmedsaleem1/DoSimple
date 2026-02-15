using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;
using Server.Utills;

namespace Server.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        JwtTokenGenerator tokenGenerator,
        IEmailService emailService,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                return null;
            }

            // Generate email verification token
            var verificationToken = GenerateSecureToken();

            // Create new user with default User role
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = PasswordHasher.HashPassword(request.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Email}", user.Email);

            // Send verification email
            var emailSent = await _emailService.SendEmailVerificationAsync(user.Email, user.Name, verificationToken);

            if (!emailSent)
            {
                _logger.LogWarning("Registration succeeded but verification email failed to send for {Email}", user.Email);
            }

            // Return message instead of token - user must verify email first
            return new RegisterResponse
            {
                Message = emailSent
                    ? "Registration successful! Please check your email to verify your account before logging in."
                    : "Registration successful! However, we could not send the verification email. Please try requesting a new verification email later.",
                Email = user.Email,
                EmailSent = emailSent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return null;
        }
    }

    public async Task<RegisterResponse?> RegisterAdminAsync(RegisterAdminRequest request)
    {
        try
        {
            // Validate admin secret key
            var configuredSecretKey = _configuration["AdminSettings:SecretKey"];
            if (string.IsNullOrEmpty(configuredSecretKey) || request.AdminSecretKey != configuredSecretKey)
            {
                _logger.LogWarning("Admin registration failed: Invalid secret key for email {Email}", request.Email);
                return null;
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("Admin registration failed: Email {Email} already exists", request.Email);
                return null;
            }

            // Generate email verification token
            var verificationToken = GenerateSecureToken();

            // Create new user with Admin role
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = PasswordHasher.HashPassword(request.Password),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin registered successfully: {Email}", user.Email);

            // Send verification email
            var emailSent = await _emailService.SendEmailVerificationAsync(user.Email, user.Name, verificationToken);

            if (!emailSent)
            {
                _logger.LogWarning("Admin registration succeeded but verification email failed to send for {Email}", user.Email);
            }

            // Return message instead of token - user must verify email first
            return new RegisterResponse
            {
                Message = emailSent
                    ? "Admin registration successful! Please check your email to verify your account before logging in."
                    : "Admin registration successful! However, we could not send the verification email. Please try requesting a new verification email later.",
                Email = user.Email,
                EmailSent = emailSent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin registration");
            return null;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        { 
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found", request.Email);
                return null;
            }

            // Verify password
            if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
            {
                _logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);
                return null;
            }

            // Check if email is verified
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login failed: Email not verified for {Email}", request.Email);
                return null;
            }

            _logger.LogInformation("User logged in successfully: {Email}", user.Email);

            // Generate token
            var token = _tokenGenerator.GenerateToken(user);
            var expiresAt = _tokenGenerator.GetTokenExpiry();

            return new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return null;
        }
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            // Check if user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Forgot password request for non-existent email: {Email}", request.Email);
                // Return true to prevent email enumeration
                return true;
            }

            // Generate password reset token
            var resetToken = GenerateSecureToken();
            
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetToken);

            _logger.LogInformation("Password reset email sent to: {Email}", user.Email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password process");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Reset password failed: User with email {Email} not found", request.Email);
                return false;
            }

            // Validate reset token
            if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetToken != request.Token)
            {
                _logger.LogWarning("Reset password failed: Invalid token for {Email}", request.Email);
                return false;
            }

            // Check if token has expired
            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Reset password failed: Expired token for {Email}", request.Email);
                return false;
            }

            // Update password
            user.Password = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send confirmation email
            await _emailService.SendPasswordChangedConfirmationAsync(user.Email, user.Name);

            _logger.LogInformation("Password reset successfully for: {Email}", user.Email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return false;
        }
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        try
        {
            // Find user with matching verification token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
            {
                _logger.LogWarning("Email verification failed: Invalid token");
                return false;
            }

            // Check if already verified
            if (user.IsEmailVerified)
            {
                _logger.LogInformation("Email already verified for: {Email}", user.Email);
                return true;
            }

            // Check if token has expired
            if (user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Email verification failed: Expired token for {Email}", user.Email);
                return false;
            }

            // Mark email as verified
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified successfully for: {Email}", user.Email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return false;
        }
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
