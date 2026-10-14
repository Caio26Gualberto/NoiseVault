using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Application.Support.Dtos;

namespace NoiseVault.Application.Support.Interfaces
{
    public interface ISupportAppService
    {
        Task<Result<SupportTicketDto>> Create(SupportTicketInputDto input);
        Task<Result<PagedResultDto<SupportTicketDto>>> ListMine(PagedRequestDto query);
        Task<Result<SupportTicketDto>> GetMine(int id);
        Task<Result<PagedResultDto<SupportMessageDto>>> MyMessages(int id, PagedRequestDto query);
        Task<Result<SupportMessageDto>> ReplyAsReporter(int id, SupportMessageInputDto input);
        Task<Result<MediaDto>> UploadPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default);
        Task<Result<VideoUploadDto>> StartVideo(StartVideoUploadInputDto input);

        Task<Result<PagedResultDto<SupportTicketAdminDto>>> List(SupportTicketListQueryDto query);
        Task<Result<SupportSummaryDto>> Summary();
        Task<Result<SupportTicketAdminDto>> Get(int id);
        Task<Result<PagedResultDto<SupportMessageDto>>> Messages(int id, PagedRequestDto query);
        Task<Result<SupportMessageDto>> ReplyAsStaff(int id, SupportMessageInputDto input);
        Task<Result<SupportTicketAdminDto>> Update(int id, SupportTicketUpdateInputDto input);
    }
}
