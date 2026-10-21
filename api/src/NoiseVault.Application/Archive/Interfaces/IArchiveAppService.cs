using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Models;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Archive.Interfaces
{
    public interface IArchiveAppService
    {
        Task<Result<PagedResultDto<ArchiveEntryDto>>> List(ArchiveListQueryDto query);
        Task<Result<PagedResultDto<ArchiveEntryDto>>> ByBand(string slug, ArchiveListQueryDto query);
        Task<Result<List<ArchiveYearDto>>> Years();
        Task<Result<ArchiveEntryDto>> Get(int id);
        Task<Result<ArchiveEntryDto>> Create(ArchiveEntryInputDto input);
        Task<Result<ArchiveEntryDto>> Update(int id, ArchiveEntryInputDto input);
        Task<Result> Delete(int id);
        Task<Result<ArchiveEntryDto>> Submit(ArchiveEntryInputDto input);
        Task<Result<ArchiveEntryDto>> UpdateSubmission(int id, ArchiveEntryInputDto input);
        Task<Result> DeleteSubmission(int id);
        Task<Result<PagedResultDto<ArchiveEntryDto>>> MySubmissions(PagedRequestDto query);
        Task<Result<PagedResultDto<ArchiveEntryDto>>> ListSubmissions(ArchiveSubmissionQueryDto query);
        Task<Result<ArchiveSubmissionSummaryDto>> SubmissionSummary();
        Task<Result<ArchiveEntryDto>> Approve(int id);
        Task<Result<ArchiveEntryDto>> Reject(int id, ArchiveRejectInputDto input);
        Task<Result<ArchiveEntryDto>> Like(int id);
        Task<Result<ArchiveEntryDto>> Unlike(int id);
        Task<Result<ArchiveEntryDto>> Attend(int id);
        Task<Result<ArchiveEntryDto>> Unattend(int id);
        Task<Result<PagedResultDto<ProfileSummaryDto>>> Attendees(int id, PagedRequestDto query);
        Task<Result<MediaDto>> UploadPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default);
        Task<Result<VideoUploadDto>> StartVideo(StartVideoUploadInputDto input);
        Task<Result<MediaDto>> RotatePhoto(int mediaId, ArchiveRotateInputDto input);

        Task<ArchiveReadModel?> FindVisible(int entryId);
        Task<ArchiveReadModel?> FindReadable(int entryId);
        Task<Error> MissingEntryError(int entryId);
        Task<List<int>> AttendeeUserIds(int entryId);
    }
}
