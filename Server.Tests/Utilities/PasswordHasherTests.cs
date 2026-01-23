using Server.Utills;

namespace Server.Tests.Utilities;

/// <summary>
/// Unit tests for the PasswordHasher utility class.
/// These tests verify that password hashing and verification work correctly.
/// </summary>
public class PasswordHasherTests
{
    #region HashPassword Tests

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedString()
    {
        // Arrange
        var password = "mySecurePassword123";

        // Act
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword); // Should not be plain text
    }

    [Fact]
    public void HashPassword_SamePassword_ReturnsDifferentHashes()
    {
        // Arrange - Hashing same password twice should give different results (due to salt)
        var password = "samePassword";

        // Act
        var hash1 = PasswordHasher.HashPassword(password);
        var hash2 = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt generates unique salt each time
    }

    [Fact]
    public void HashPassword_EmptyPassword_ReturnsHash()
    {
        // Arrange
        var password = "";

        // Act
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
    }

    [Fact]
    public void HashPassword_LongPassword_ReturnsHash()
    {
        // Arrange
        var password = new string('a', 1000); // Very long password

        // Act
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
    }

    [Fact]
    public void HashPassword_SpecialCharacters_ReturnsHash()
    {
        // Arrange
        var password = "P@$$w0rd!#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
    }

    [Fact]
    public void HashPassword_UnicodeCharacters_ReturnsHash()
    {
        // Arrange
        var password = "пароль密码كلمة";

        // Act
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "correctPassword123";
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Act
        var result = PasswordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var originalPassword = "correctPassword";
        var wrongPassword = "wrongPassword";
        var hashedPassword = PasswordHasher.HashPassword(originalPassword);

        // Act
        var result = PasswordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_CaseSensitive_ReturnsFalse()
    {
        // Arrange
        var password = "Password123";
        var wrongCasePassword = "password123"; // Different case
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Act
        var result = PasswordHasher.VerifyPassword(wrongCasePassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalseForNonEmptyHash()
    {
        // Arrange
        var password = "actualPassword";
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Act
        var result = PasswordHasher.VerifyPassword("", hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_PasswordWithWhitespace_ReturnsTrue()
    {
        // Arrange
        var password = "password with spaces";
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Act
        var result = PasswordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_TrimmedPassword_ReturnsFalse()
    {
        // Arrange
        var password = " password ";
        var hashedPassword = PasswordHasher.HashPassword(password);

        // Act - Verify with trimmed version (should fail)
        var result = PasswordHasher.VerifyPassword("password", hashedPassword);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Theory]
    [InlineData("simple")]
    [InlineData("Complex123!")]
    [InlineData("verylongpasswordthatissecure")]
    [InlineData("12345")]
    [InlineData("!@#$%")]
    public void HashAndVerify_VariousPasswords_WorksCorrectly(string password)
    {
        // Arrange & Act
        var hashedPassword = PasswordHasher.HashPassword(password);
        var isValid = PasswordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void PasswordHasher_IsThreadSafe()
    {
        // Arrange
        var passwords = Enumerable.Range(0, 100).Select(i => $"password{i}").ToList();

        // Act - Hash and verify in parallel
        var results = passwords.AsParallel().Select(password =>
        {
            var hash = PasswordHasher.HashPassword(password);
            return PasswordHasher.VerifyPassword(password, hash);
        }).ToList();

        // Assert
        Assert.All(results, result => Assert.True(result));
    }

    #endregion
}
