using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Application.Media.Interfaces
{
    public interface IMediaUploadAppService
    {
        Task<Result<MediaDto>> UploadPhoto(ImageUploadInput? file, EMediaProfile profile, CancellationToken cancellationToken = default);
        Task<Result<VideoUploadDto>> StartVideo(StartVideoUploadInputDto input, EMediaProfile profile);
        Task<Result<MediaDto>> CompleteVideo(int id, CompleteVideoUploadInputDto input);
        Task<Result<MediaDto>> Get(int id);
        Task<Result> Delete(int id);

        Task<List<MediaUpload>> ListAttachable(IReadOnlyCollection<int> ids, int ownerUserId, EMediaProfile profile);
        MediaDto Describe(MediaUpload media);
        Task<Error?> RotatePhoto(MediaUpload media, int degrees);
        Task DeleteStoredFiles(IReadOnlyCollection<MediaUpload> media);
        Task Discard(IReadOnlyCollection<int> mediaIds);
        Task<EMediaStatus?> ProcessVideo(int mediaId, bool finalAttempt);
        Task<List<int>> PollRemoteTranscodes();
        Task<int> Cleanup();
    }
}
