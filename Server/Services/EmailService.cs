using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Server.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _appUrl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var emailSettings = _configuration.GetSection("Email");
        _smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
        _smtpUsername = emailSettings["SmtpUsername"] ?? "";
        _smtpPassword = emailSettings["SmtpPassword"] ?? "";
        _fromEmail = emailSettings["FromEmail"] ?? "";
        _fromName = emailSettings["FromName"] ?? "DoSimple";
        _appUrl = emailSettings["AppUrl"] ?? "http://localhost:5248";

        // Log configuration for debugging (mask password)
        _logger.LogInformation("Email Service configured: Host={Host}, Port={Port}, Username={Username}, HasPassword={HasPassword}", 
            _smtpHost, _smtpPort, _smtpUsername, !string.IsNullOrEmpty(_smtpPassword));
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken)
    {
        var subject = "Verify Your Email - DoSimple";
        var verificationLink = $"{_appUrl}/api/auth/verify-email?token={verificationToken}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to DoSimple!</h1>
        </div>
        <div class='content'>
            <h2>Hi {userName},</h2>
            <p>Thank you for registering with DoSimple. Please verify your email address to activate your account.</p>
            <p>Click the button below to verify your email:</p>
            <a href='{verificationLink}' class='button'>Verify Email</a>
            <p>Or copy and paste this link into your browser:</p>
            <p>{verificationLink}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 DoSimple. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        var subject = "Password Reset Request - DoSimple";
        var resetLink = $"{_appUrl}/api/auth/reset-password?token={resetToken}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #FF9800; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #777; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hi {userName},</h2>
            <p>We received a request to reset your password for your DoSimple account.</p>
            <p>Click the button below to reset your password:</p>
            <a href='{resetLink}' class='button'>Reset Password</a>
            <p>Or copy and paste this link into your browser:</p>
            <p>{resetLink}</p>
            <div class='warning'>
                <strong>Security Notice:</strong>
                <ul>
                    <li>This link will expire in 1 hour</li>
                    <li>If you didn't request this reset, please ignore this email</li>
                    <li>Your password will remain unchanged until you create a new one</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>&copy; 2026 DoSimple. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendPasswordChangedConfirmationAsync(string toEmail, string userName)
    {
        var subject = "Password Changed Successfully - DoSimple";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #777; }}
        .alert {{ background-color: #d4edda; border-left: 4px solid #28a745; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Changed</h1>
        </div>
        <div class='content'>
            <h2>Hi {userName},</h2>
            <div class='alert'>
                <strong>Success!</strong> Your password has been changed successfully.
            </div>
            <p>If you made this change, no further action is required.</p>
            <p>If you did not change your password, please contact our support team immediately as your account may be compromised.</p>
            <p><strong>Date/Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 DoSimple. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            // Check if email is configured
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("Email service not configured. Email not sent to {Email}", toEmail);
                _logger.LogInformation("Email would have been sent:\nTo: {To}\nSubject: {Subject}", toEmail, subject);
                return true; // Return true in development to not block the flow
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
