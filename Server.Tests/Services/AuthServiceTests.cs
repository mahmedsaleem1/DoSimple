using Moq;
using Server.Data;
using Server.DTOs;
using Server.Models;
using Server.Services;
using Server.Tests.Helpers;
using Server.Utills;

namespace Server.Tests.Services;

/// <summary>
/// Unit tests for the AuthService class.
/// These tests verify registration, login, password reset, and email verification functionality.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup - Create fresh in-memory database for each test
        _context = TestDbContextFactory.CreateInMemoryContext();
        _emailServiceMock = MockServiceFactory.CreateEmailServiceMock();
        var configuration = TestConfigurationFactory.CreateTestConfiguration();
        _tokenGenerator = new JwtTokenGenerator(configuration);
        var logger = TestConfigurationFactory.CreateMockLogger<AuthService>();

        _authService = new AuthService(
            _context,
            _tokenGenerator,
            _emailServiceMock.Object,
            logger,
            configuration
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Register Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsRegisterResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser@example.com", result.Email);
        Assert.Contains("successful", result.Message.ToLower());
        
        // Verify user was created in database
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
        Assert.NotNull(user);
        Assert.Equal("User", user.Role);
        Assert.False(user.IsEmailVerified);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsNull()
    {
        // Arrange - First create a user
        var existingUser = new User
        {
            Name = "Existing User",
            Email = "existing@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Name = "Another User",
            Email = "existing@example.com", // Same email
            Password = "differentpassword"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_SendsVerificationEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = "password123"
        };

        // Act
        await _authService.RegisterAsync(request);

        // Assert - Verify email service was called
        _emailServiceMock.Verify(
            x => x.SendEmailVerificationAsync(
                request.Email,
                request.Name,
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Register Admin Tests

    [Fact]
    public async Task RegisterAdminAsync_WithValidSecretKey_ReturnsRegisterResponse()
    {
        // Arrange
        var request = new RegisterAdminRequest
        {
            Name = "New Admin",
            Email = "newadmin@example.com",
            Password = "adminpass123",
            AdminSecretKey = "TestAdminSecretKey123" // Must match configuration
        };

        // Act
        var result = await _authService.RegisterAdminAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newadmin@example.com", result.Email);
        
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
        Assert.NotNull(user);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public async Task RegisterAdminAsync_WithInvalidSecretKey_ReturnsNull()
    {
        // Arrange
        var request = new RegisterAdminRequest
        {
            Name = "New Admin",
            Email = "newadmin@example.com",
            Password = "adminpass123",
            AdminSecretKey = "WrongSecretKey"
        };

        // Act
        var result = await _authService.RegisterAdminAsync(request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentialsAndVerifiedEmail_ReturnsAuthResponse()
    {
        // Arrange - Create a verified user
        var hashedPassword = PasswordHasher.HashPassword("correctpassword");
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword,
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "correctpassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("User", result.Role);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = PasswordHasher.HashPassword("correctpassword"),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "unverified@example.com",
            Password = PasswordHasher.HashPassword("password123"),
            Role = "User",
            IsEmailVerified = false, // Not verified
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "unverified@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "anypassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Email Verification Tests

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var verificationToken = "valid-token-123";
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.VerifyEmailAsync(verificationToken);

        // Assert
        Assert.True(result);
        
        var updatedUser = _context.Users.First(u => u.Email == "test@example.com");
        Assert.True(updatedUser.IsEmailVerified);
        Assert.Null(updatedUser.EmailVerificationToken);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var verificationToken = "expired-token";
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1), // Expired
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.VerifyEmailAsync(verificationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var result = await _authService.VerifyEmailAsync("nonexistent-token");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Forgot Password Tests

    [Fact]
    public async Task ForgotPasswordAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = PasswordHasher.HashPassword("password"),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ForgotPasswordRequest { Email = "test@example.com" };

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        Assert.True(result);
        _emailServiceMock.Verify(
            x => x.SendPasswordResetEmailAsync(
                request.Email,
                user.Name,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_StillReturnsTrueForSecurity()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "nonexistent@example.com" };

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert - Returns true to prevent email enumeration (security best practice)
        Assert.True(result);
        
        // Verify email was NOT sent (no user to send to)
        _emailServiceMock.Verify(
            x => x.SendPasswordResetEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    #endregion
}
