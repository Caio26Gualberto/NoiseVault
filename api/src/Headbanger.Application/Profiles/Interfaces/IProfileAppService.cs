using Headbanger.Application.Profiles.Dtos;
using Headbanger.Application.Response;

namespace Headbanger.Application.Profiles.Interfaces
{
    public interface IProfileAppService
    {
        Task<Result<ProfileDto>> Me();
        Task<Result<ProfileDto>> UpdateName(UpdateProfileNameDto input);
    }
}
