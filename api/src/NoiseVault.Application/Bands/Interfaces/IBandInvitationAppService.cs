using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandInvitationAppService
    {
        Task<Result<BandInvitationDto>> Invite(string slug, InviteMemberInputDto input);
        Task<Result<PagedResultDto<BandInvitationDto>>> ListForBand(string slug, PagedRequestDto query);
        Task<Result<PagedResultDto<BandInvitationDto>>> ListMine(PagedRequestDto query);
        Task<Result<BandInvitationDto>> Accept(int invitationId);
        Task<Result<BandInvitationDto>> Decline(int invitationId);
        Task<Result<BandInvitationDto>> Revoke(int invitationId);
    }
}
