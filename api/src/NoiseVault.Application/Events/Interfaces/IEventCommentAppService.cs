using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Events.Interfaces
{
    public interface IEventCommentAppService
    {
        Task<Result<PagedResultDto<EventCommentDto>>> List(int eventId, PagedRequestDto query);
        Task<Result<EventCommentDto>> Add(int eventId, CommentInputDto input);
        Task<Result> Delete(int id);
    }
}
