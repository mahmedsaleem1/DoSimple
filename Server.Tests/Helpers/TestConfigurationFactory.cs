using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Server.Tests.Helpers;

/// <summary>
/// Factory class to create test configurations and loggers
/// </summary>
public static class TestConfigurationFactory
{
    /// <summary>
    /// Creates a test IConfiguration with JWT and Admin settings
    /// </summary>
    public static IConfiguration CreateTestConfiguration()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", "TestSecretKeyForJwtTokenGeneration12345678901234567890" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpiryInMinutes", "60" },
            { "AdminSettings:SecretKey", "TestAdminSecretKey123" },
            { "Email:SmtpHost", "smtp.test.com" },
            { "Email:SmtpPort", "587" },
            { "Email:SenderEmail", "test@example.com" },
            { "Email:SenderName", "Test Sender" },
            { "FrontendUrl", "http://localhost:3000" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    /// <summary>
    /// Creates a mock logger for any type
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    /// <summary>
    /// Creates a mock logger that can be verified for calls
    /// </summary>
    public static Mock<ILogger<T>> CreateVerifiableMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}
