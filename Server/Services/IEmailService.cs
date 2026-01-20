namespace Server.Services;

public interface IEmailService
{
    Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
    Task<bool> SendPasswordChangedConfirmationAsync(string toEmail, string userName);
}
