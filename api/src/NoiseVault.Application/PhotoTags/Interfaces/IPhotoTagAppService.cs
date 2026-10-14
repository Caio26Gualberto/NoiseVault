using NoiseVault.Application.PhotoTags.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.PhotoTags.Interfaces
{
    public interface IPhotoTagAppService
    {
        Task<Result<List<PhotoTagDto>>> Add(int mediaId, PhotoTagInputDto input);
        Task<Result> Remove(int tagId);

        Task<Dictionary<int, List<PhotoTagDto>>> ForMedia(IReadOnlyCollection<int> mediaIds);
    }
}
