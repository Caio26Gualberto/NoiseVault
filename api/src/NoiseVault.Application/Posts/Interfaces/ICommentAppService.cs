using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Posts.Interfaces
{
    public interface ICommentAppService
    {
        Task<Result<PagedResultDto<CommentDto>>> List(int postId, PagedRequestDto query);
        Task<Result<CommentDto>> Add(int postId, CommentInputDto input);
        Task<Result> Delete(int id);
    }
}
