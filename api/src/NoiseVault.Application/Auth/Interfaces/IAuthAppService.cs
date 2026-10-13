using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Auth.Interfaces
{
    public interface IAuthAppService
    {
        Task<Result<LoginResponseDto>> Login(LoginInputDto input);
        Task<Result<RegistrationPendingDto>> Register(RegisterInputDto input);
        Task<Result<ConfirmEmailResultDto>> ConfirmEmail(ConfirmEmailInputDto input);
        Task<Result> ResendConfirmation(ResendConfirmationInputDto input);
        Task<Result> ForgotPassword(ForgotPasswordInputDto input);
        Task<Result> ResetPassword(ResetPasswordInputDto input);
        Task<Result<TokensDto>> RefreshTokens(RefreshTokenRequestDto input);
        Task<Result<bool>> Logout();
        Task<Result<TokensDto>> ChangePassword(ChangePasswordInputDto input);
        Task<Result<TokensDto>> SetInitialPassword(SetInitialPasswordInputDto input);
    }
}
