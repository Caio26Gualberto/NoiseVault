using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Posts.Models;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Posts.Interfaces
{
    public interface IPostAppService
    {
        Task<Result<PagedResultDto<PostDto>>> Feed(PagedRequestDto query);
        Task<Result<PagedResultDto<PostDto>>> ByUser(string nickname, PagedRequestDto query);
        Task<Result<PagedResultDto<PostDto>>> ByBand(string slug, PagedRequestDto query);
        Task<Result<PostDto>> Get(int id);
        Task<Result<PostDto>> Create(CreatePostInputDto input);
        Task<Result<PostDto>> Update(int id, UpdatePostInputDto input);
        Task<Result> Delete(int id);
        Task<Result<PostDto>> Like(int id);
        Task<Result<PostDto>> Unlike(int id);

        Task<PostReadModel?> FindVisible(int postId);
        Task ProcessVideo(int mediaUploadId, bool finalAttempt);
        Task CreateForEvent(int eventId, int authorUserId, int? bandId);
        Task DeleteForEvent(int eventId);
    }
}
