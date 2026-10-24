using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandPartnershipAppService
    {
        Task<Result<BandPartnershipDto>> Invite(string slug, InvitePartnerInputDto input);
        Task<Result<PagedResultDto<BandPartnershipDto>>> ListAccepted(string slug, PagedRequestDto query);
        Task<Result<PagedResultDto<BandPartnershipDto>>> ListPending(string slug, PagedRequestDto query);
        Task<Result<BandPartnershipDto>> Accept(int partnershipId);
        Task<Result<BandPartnershipDto>> Decline(int partnershipId);
        Task<Result> Remove(int partnershipId);
    }
}
