using NoiseVault.Application.BandOpenings.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.BandOpenings.Interfaces
{
    public interface IBandOpeningAppService
    {
        Task<Result<BandOpeningDto>> Create(string slug, BandOpeningInputDto input);
        Task<Result<BandOpeningDto>> Update(int id, BandOpeningInputDto input);
        Task<Result<BandOpeningDto>> Close(int id, CloseOpeningInputDto input);
        Task<Result<BandOpeningDto>> Renew(int id);
        Task<Result> Delete(int id);
        Task<Result<BandOpeningDto>> Get(int id);
        Task<Result<PagedResultDto<BandOpeningDto>>> Search(BandOpeningSearchQueryDto query);
        Task<Result<PagedResultDto<BandOpeningDto>>> ListForBand(string slug, PagedRequestDto query);

        Task<Result<BandOpeningDto>> ExpressInterest(int id, OpeningInterestInputDto input);
        Task<Result<BandOpeningDto>> WithdrawInterest(int id);
        Task<Result<PagedResultDto<OpeningInterestDto>>> ListInterests(int id, PagedRequestDto query);
        Task<Result<OpeningInterestDto>> InviteInterested(int id, int interestId, InviteFromInterestInputDto input);
        Task<Result<OpeningInterestDto>> DismissInterested(int id, int interestId);
    }
}
