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
/// Unit tests for the AuthController class.
/// These tests verify that the controller correctly handles HTTP requests
/// and returns appropriate responses based on service results.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123"
        };

        var response = new RegisterResponse
        {
            Message = "Registration successful!",
            Email = request.Email
        };

        _authServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<RegisterResponse>(okResult.Value);
        Assert.Equal(request.Email, returnedResponse.Email);
    }

    [Fact]
    public async Task Register_ServiceReturnsNull_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "password123"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync((RegisterResponse?)null);

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest();
        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region RegisterAdmin Tests

    [Fact]
    public async Task RegisterAdmin_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RegisterAdminRequest
        {
            Name = "Admin User",
            Email = "admin@example.com",
            Password = "adminpass123",
            AdminSecretKey = "secret"
        };

        var response = new RegisterResponse
        {
            Message = "Admin registration successful!",
            Email = request.Email
        };

        _authServiceMock.Setup(x => x.RegisterAdminAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RegisterAdmin(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RegisterAdmin_InvalidSecretKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterAdminRequest
        {
            Name = "Admin User",
            Email = "admin@example.com",
            Password = "adminpass123",
            AdminSecretKey = "wrongkey"
        };

        _authServiceMock.Setup(x => x.RegisterAdminAsync(request))
            .ReturnsAsync((RegisterResponse?)null);

        // Act
        var result = await _controller.RegisterAdmin(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var response = new AuthResponse
        {
            Token = "jwt-token-here",
            Email = request.Email,
            Name = "Test User",
            Role = "User",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.NotEmpty(authResponse.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync((AuthResponse?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest();
        _controller.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_ExistingEmail_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "test@example.com" };

        _authServiceMock.Setup(x => x.ForgotPasswordAsync(request))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region VerifyEmail Tests

    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsOk()
    {
        // Arrange
        var token = "valid-token";
        _authServiceMock.Setup(x => x.VerifyEmailAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var token = "invalid-token";
        _authServiceMock.Setup(x => x.VerifyEmailAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
