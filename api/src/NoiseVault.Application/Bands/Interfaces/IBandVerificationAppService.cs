using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandVerificationAppService
    {
        Task<Result<BandVerificationDto>> Request(string slug, BandVerificationRequestInputDto input);
        Task<Result<PagedResultDto<BandVerificationDto>>> List(BandVerificationQueryDto query);
        Task<Result<BandVerificationSummaryDto>> Summary();
        Task<Result<BandVerificationDto>> Approve(int id);
        Task<Result<BandVerificationDto>> Reject(int id, BandVerificationRejectInputDto input);
        Task<Result<BandProfileDto>> Unverify(int bandId);
        Task<BandVerificationStateDto?> StateForBand(int bandId);
        Task SendRequestedEmail(int requestId);
    }
}
