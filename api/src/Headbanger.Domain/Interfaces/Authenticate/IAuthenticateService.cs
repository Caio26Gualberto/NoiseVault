using Headbanger.Domain.Entities;

namespace Headbanger.Domain.Interfaces.Authenticate
{
    public interface IAuthenticateService
    {
        Task<(bool, bool)> Authenticate(string email, string password);
        Task<UserCreationResult> RegisterUser(string email, string password, string name);
        Task<UserCreationResult> CreateUser(string email, string temporaryPassword, string name, string role);
        Task Logout(string email);
        Task<bool> ChangePassword(string email, string password);
        Task<bool> IsPasswordChangeRequired(string email);

        Task<string> GenerateJwtToken(User user);
        Task<string> IssueRefreshTokenAsync(string email);
        Task<string?> ConsumeRefreshTokenAsync(string refreshToken);
    }
}
