using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Events.Models;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Events.Interfaces
{
    public interface IEventAppService
    {
        Task<Result<PagedResultDto<EventDto>>> List(EventListQueryDto query);
        Task<Result<List<EventDto>>> Upcoming(int take);
        Task<Result<PagedResultDto<EventDto>>> ByBand(string slug, EventListQueryDto query);
        Task<Result<PagedResultDto<EventDto>>> ByUser(string nickname, EventListQueryDto query);
        Task<Result<EventDto>> Get(int id);
        Task<Result<EventDto>> Create(CreateEventInputDto input);
        Task<Result<EventDto>> Update(int id, EventInputDto input);
        Task<Result<EventDto>> Cancel(int id);
        Task<Result> Delete(int id);
        Task<Result<EventDto>> AddInterest(int id);
        Task<Result<EventDto>> RemoveInterest(int id);
        Task<Result<EventDto>> SetBanner(int id, ImageUploadInput? file, int? position, CancellationToken cancellationToken = default);
        Task<Result<EventDto>> SetBannerPosition(int id, BannerPositionInputDto input);
        Task<Result<EventDto>> RemoveBanner(int id);

        Task<EventReadModel?> FindVisible(int eventId);
        Task<int> RemindTomorrow();
    }
}
