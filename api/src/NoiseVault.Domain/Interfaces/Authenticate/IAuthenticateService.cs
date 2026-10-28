using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Auth;

namespace NoiseVault.Domain.Interfaces.Authenticate
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

        Task<int?> FindUserIdByLoginAsync(EExternalProvider provider, string providerKey);
        Task<UserCreationResult> RegisterExternalUser(string email, bool emailConfirmed, string name, string nickname, EExternalProvider provider, string providerKey);
        Task<bool> AddLoginAsync(int userId, EExternalProvider provider, string providerKey);
        Task<bool> RemoveLoginAsync(int userId, EExternalProvider provider);
        Task<SignInMethods> GetSignInMethodsAsync(int userId);
        Task<IReadOnlyDictionary<int, SignInMethods>> GetSignInMethodsAsync(IReadOnlyCollection<int> userIds);
        Task<int> CountUnconfirmedAsync();
        Task ClaimUnconfirmedAccountAsync(int userId);
        Task MarkEmailConfirmedAsync(int userId);
        Task<EPasswordChangeResult> CreatePasswordAsync(int userId, string newPassword);
    }
}
