using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Archive.Interfaces
{
    public interface IArchiveCommentAppService
    {
        Task<Result<PagedResultDto<ArchiveCommentDto>>> List(int entryId, PagedRequestDto query);
        Task<Result<ArchiveCommentDto>> Add(int entryId, CommentInputDto input);
        Task<Result> Delete(int id);
    }
}
