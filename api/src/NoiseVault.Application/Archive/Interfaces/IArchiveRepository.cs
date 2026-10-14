using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Archive.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Archive;

namespace NoiseVault.Application.Archive.Interfaces
{
    public interface IArchiveRepository : IRepository<ArchiveEntry>
    {
        Task<(List<ArchiveReadModel> Items, int TotalCount)> PageAsync(ArchiveFilter filter, int viewerUserId, int skip, int take);
        Task<ArchiveReadModel?> FindAsync(int entryId, int viewerUserId, bool includeUnpublished = false);
        Task<(List<ArchiveReadModel> Items, int TotalCount)> PageSubmissionsAsync(EArchiveEntryStatus? status, int? authorUserId, string? search, int viewerUserId, int skip, int take);
        Task<int> CountPendingAsync(int authorUserId);
        Task<Dictionary<EArchiveEntryStatus, int>> CountSubmissionsByStatusAsync();
        Task<bool> TryReviewAsync(int entryId, EArchiveEntryStatus status, int reviewerUserId, string? rejectionReason, DateTime reviewedAt);
        Task<List<ArchiveReadModel>> ListByIdsAsync(IReadOnlyCollection<int> entryIds, int viewerUserId);
        Task<List<(int Year, int Count)>> YearsAsync();
        Task<bool> AddLikeAsync(int entryId, int userId);
        Task RemoveLikeAsync(int entryId, int userId);
        Task<bool> AddAttendanceAsync(int entryId, int userId);
        Task RemoveAttendanceAsync(int entryId, int userId);
        Task<List<int>> AttendeeUserIdsAsync(int entryId);
        Task<(List<User> Users, int TotalCount)> PageAttendeesAsync(int entryId, int skip, int take);
    }
}
