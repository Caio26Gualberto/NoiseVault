using Headbanger.Domain.Entities;

namespace Headbanger.Domain.Interfaces.Authenticate
{
    public interface IAuthenticateService
    {
        Task<(bool, bool)> Authenticate(string email, string password);
        Task<UserCreationResult> RegisterUser(string email, string password, string name, string nickname);
        Task<UserCreationResult> CreateUser(string email, string temporaryPassword, string name, string nickname, string role);
        Task Logout(string email);
        Task<EPasswordChangeResult> ChangePassword(string email, string currentPassword, string newPassword);
        Task<EPasswordChangeResult> SetInitialPassword(string email, string newPassword);
        Task<bool> IsPasswordChangeRequired(string email);

        Task<bool> IsEmailConfirmed(string email);
        Task<string?> GenerateEmailConfirmationTokenAsync(int userId);
        Task<EEmailConfirmationResult> ConfirmEmailAsync(int userId, string token);
        Task<string?> GeneratePasswordResetTokenAsync(string email);
        Task<EPasswordResetResult> ResetPasswordAsync(string email, string token, string newPassword);

        Task<string> GenerateJwtToken(User user);
        Task<string> IssueRefreshTokenAsync(string email);
        Task<string?> ConsumeRefreshTokenAsync(string refreshToken);
    }
}
