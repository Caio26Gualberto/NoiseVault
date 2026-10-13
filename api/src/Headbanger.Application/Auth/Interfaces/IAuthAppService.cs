using Headbanger.Application.Auth.Dtos;
using Headbanger.Application.Response;

namespace Headbanger.Application.Auth.Interfaces
{
    public interface IAuthAppService
    {
        Task<Result<LoginResponseDto>> Login(LoginInputDto input);
        Task<Result<LoginResponseDto>> Register(RegisterInputDto input);
        Task<Result<TokensDto>> RefreshTokens(RefreshTokenRequestDto input);
        Task<Result<bool>> Logout();
        Task<Result<TokensDto>> ChangePassword(ChangePasswordInputDto input);
        Task<Result<TokensDto>> SetInitialPassword(SetInitialPasswordInputDto input);
    }
}
