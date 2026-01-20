using Server.DTOs;

namespace Server.Services;

public interface IAuthService
{
    Task<RegisterResponse?> RegisterAsync(RegisterRequest request);
    Task<RegisterResponse?> RegisterAdminAsync(RegisterAdminRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> VerifyEmailAsync(string token);
}
