using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Server.Models;
using Server.Tests.Helpers;
using Server.Utills;

namespace Server.Tests.Utilities;

/// <summary>
/// Unit tests for the JwtTokenGenerator utility class.
/// These tests verify JWT token generation and configuration.
/// </summary>
public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _tokenGenerator;

    public JwtTokenGeneratorTests()
    {
        var configuration = TestConfigurationFactory.CreateTestConfiguration();
        _tokenGenerator = new JwtTokenGenerator(configuration);
    }

    #region GenerateToken Tests

    [Fact]
    public void GenerateToken_ValidUser_ReturnsNonEmptyToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsValidJwtFormat()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);

        // Assert - JWT has 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateToken_ContainsUserIdClaim()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal("42", subClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("test@example.com", emailClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsNameClaim()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "John Doe",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("John Doe", nameClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaim()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "admin@example.com",
            Name = "Admin User",
            Role = "Admin"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsJtiClaim()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert - JTI is a unique identifier for each token
        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jtiClaim);
        Assert.True(Guid.TryParse(jtiClaim.Value, out _)); // Should be a valid GUID
    }

    [Fact]
    public void GenerateToken_TokenHasExpiration()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwtToken.ValidTo);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectIssuer()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal("TestIssuer", jwtToken.Issuer);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectAudience()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Contains("TestAudience", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_DifferentUsers_GenerateDifferentTokens()
    {
        // Arrange
        var user1 = new User { Id = 1, Email = "user1@example.com", Name = "User 1", Role = "User" };
        var user2 = new User { Id = 2, Email = "user2@example.com", Name = "User 2", Role = "User" };

        // Act
        var token1 = _tokenGenerator.GenerateToken(user1);
        var token2 = _tokenGenerator.GenerateToken(user2);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_SameUserMultipleTimes_GeneratesDifferentTokens()
    {
        // Arrange - Due to JTI (unique identifier), same user gets different tokens
        var user = new User { Id = 1, Email = "test@example.com", Name = "Test", Role = "User" };

        // Act
        var token1 = _tokenGenerator.GenerateToken(user);
        var token2 = _tokenGenerator.GenerateToken(user);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    #endregion

    #region GetTokenExpiry Tests

    [Fact]
    public void GetTokenExpiry_ReturnsFutureDate()
    {
        // Act
        var expiry = _tokenGenerator.GetTokenExpiry();

        // Assert
        Assert.True(expiry > DateTime.UtcNow);
    }

    [Fact]
    public void GetTokenExpiry_ReturnsConfiguredExpiry()
    {
        // Arrange - Configuration has 60 minutes expiry
        var expectedExpiryApprox = DateTime.UtcNow.AddMinutes(60);

        // Act
        var expiry = _tokenGenerator.GetTokenExpiry();

        // Assert - Allow 1 minute tolerance
        Assert.True(Math.Abs((expiry - expectedExpiryApprox).TotalMinutes) < 1);
    }

    #endregion

    #region Token Validation Integration Tests

    [Fact]
    public void GeneratedToken_CanBeDecodedByHandler()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", Name = "Test", Role = "User" };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();

        // Assert
        Assert.True(handler.CanReadToken(token));
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public void GenerateToken_DifferentRoles_ContainsCorrectRole(string role)
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = role
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(role, roleClaim.Value);
    }

    #endregion
}
