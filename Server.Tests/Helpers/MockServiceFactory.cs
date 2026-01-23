using Moq;
using Server.Services;

namespace Server.Tests.Helpers;

/// <summary>
/// Factory class to create mock services for testing.
/// Using Moq library to create test doubles for external dependencies.
/// </summary>
public static class MockServiceFactory
{
    /// <summary>
    /// Creates a mock IEmailService that returns success for all operations
    /// </summary>
    public static Mock<IEmailService> CreateEmailServiceMock(bool shouldSucceed = true)
    {
        var mock = new Mock<IEmailService>();
        
        mock.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(shouldSucceed);
        
        mock.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(shouldSucceed);
        
        mock.Setup(x => x.SendPasswordChangedConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(shouldSucceed);
        
        return mock;
    }

    /// <summary>
    /// Creates a mock ICloudinaryService that returns a test URL or null based on configuration
    /// </summary>
    public static Mock<ICloudinaryService> CreateCloudinaryServiceMock(bool shouldSucceed = true)
    {
        var mock = new Mock<ICloudinaryService>();
        
        mock.Setup(x => x.UploadImageAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(shouldSucceed ? "https://test-cloudinary.com/image.jpg" : null);
        
        mock.Setup(x => x.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(shouldSucceed);
        
        return mock;
    }
}
