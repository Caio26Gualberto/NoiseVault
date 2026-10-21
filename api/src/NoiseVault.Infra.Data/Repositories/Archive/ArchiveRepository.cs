using NoiseVault.Application.Cities.Dtos;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Archive.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Archive;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.Archive
{
    public class ArchiveRepository : Repository<ArchiveEntry>, IArchiveRepository
    {
        public ArchiveRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(List<ArchiveReadModel> Items, int TotalCount)> PageAsync(ArchiveFilter filter, int viewerUserId, int skip, int take)
        {
            var query = Visible();

            if (filter.Year != null)
                query = query.Where(e => e.Year == filter.Year);

            if (filter.Decade != null)
                query = query.Where(e => e.Year >= filter.Decade && e.Year < filter.Decade + 10);

            if (filter.CityId != null)
                query = query.Where(e => e.CityId == filter.CityId);

            if (filter.Kind != null)
                query = query.Where(e => e.Media.Any(m => !m.IsDeleted && m.MediaUpload!.Kind == filter.Kind));

            if (filter.BandId != null)
                query = query.Where(e => e.Bands.Any(b => !b.IsDeleted && b.BandId == filter.BandId));

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(term)
                    || (e.Description != null && e.Description.ToLower().Contains(term))
                    || (e.Venue != null && e.Venue.ToLower().Contains(term))
                    || (e.CityName != null && e.CityName.ToLower().Contains(term))
                    || (e.City != null && e.City.Name.ToLower().Contains(term))
                    || e.Bands.Any(b => !b.IsDeleted
                        && ((b.Name != null && b.Name.ToLower().Contains(term))
                            || (b.Band != null && b.Band.Name.ToLower().Contains(term)))));
            }

            var totalCount = await query.CountAsync();

            var ordered = filter.OldestFirst
                ? query.OrderBy(e => e.SortDate).ThenBy(e => e.Id)
                : query.OrderByDescending(e => e.SortDate).ThenByDescending(e => e.Id);

            var items = await Project(ordered.Skip(skip).Take(take), viewerUserId).ToListAsync();

            return (items, totalCount);
        }

        public Task<ArchiveReadModel?> FindAsync(int entryId, int viewerUserId, bool includeUnpublished = false) =>
            Project((includeUnpublished ? Existing() : Visible()).Where(e => e.Id == entryId), viewerUserId).FirstOrDefaultAsync();

        public async Task<(List<ArchiveReadModel> Items, int TotalCount)> PageSubmissionsAsync(EArchiveEntryStatus? status, int? authorUserId, string? search, int viewerUserId, int skip, int take)
        {
            var query = Existing().Where(e => e.IsSubmission);

            if (status != null)
                query = query.Where(e => e.Status == status);

            if (authorUserId != null)
                query = query.Where(e => e.AuthorUserId == authorUserId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(term)
                    || e.Author!.Name.ToLower().Contains(term)
                    || e.Author.Nickname.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var ordered = status == EArchiveEntryStatus.Pending
                ? query.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
                : query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id);

            var items = await Project(ordered.Skip(skip).Take(take), viewerUserId).ToListAsync();

            return (items, totalCount);
        }

        public Task<int> CountPendingAsync(int authorUserId) =>
            Existing().CountAsync(e => e.AuthorUserId == authorUserId && e.Status == EArchiveEntryStatus.Pending);

        public async Task<Dictionary<EArchiveEntryStatus, int>> CountSubmissionsByStatusAsync()
        {
            var rows = await Existing()
                .Where(e => e.IsSubmission)
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(r => r.Status, r => r.Count);
        }

        public async Task<bool> TryReviewAsync(int entryId, EArchiveEntryStatus status, int reviewerUserId, string? rejectionReason, DateTime reviewedAt) =>
            await _dbSet
                .Where(e => e.Id == entryId && !e.IsDeleted && e.Status == EArchiveEntryStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, status)
                    .SetProperty(e => e.ReviewedByUserId, reviewerUserId)
                    .SetProperty(e => e.ReviewedAt, reviewedAt)
                    .SetProperty(e => e.RejectionReason, rejectionReason)
                    .SetProperty(e => e.UpdatedAt, reviewedAt)
                    .SetProperty(e => e.UpdatedBy, reviewerUserId)) == 1;

        public Task<List<ArchiveReadModel>> ListByIdsAsync(IReadOnlyCollection<int> entryIds, int viewerUserId) =>
            Project(Visible().Where(e => entryIds.Contains(e.Id)), viewerUserId).ToListAsync();

        public async Task<List<(int Year, int Count)>> YearsAsync()
        {
            var rows = await Visible()
                .GroupBy(e => e.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(r => r.Year)
                .ToListAsync();

            return rows.Select(r => (r.Year, r.Count)).ToList();
        }

        public Task<bool> AddLikeAsync(int entryId, int userId) =>
            AddLinkAsync(new ArchiveLike { ArchiveEntryId = entryId, UserId = userId }, l => l.ArchiveEntryId == entryId && l.UserId == userId);

        public Task RemoveLikeAsync(int entryId, int userId) =>
            _context.Set<ArchiveLike>().Where(l => l.ArchiveEntryId == entryId && l.UserId == userId).ExecuteDeleteAsync();

        public Task<bool> AddAttendanceAsync(int entryId, int userId) =>
            AddLinkAsync(new ArchiveAttendance { ArchiveEntryId = entryId, UserId = userId }, a => a.ArchiveEntryId == entryId && a.UserId == userId);

        public Task RemoveAttendanceAsync(int entryId, int userId) =>
            _context.Set<ArchiveAttendance>().Where(a => a.ArchiveEntryId == entryId && a.UserId == userId).ExecuteDeleteAsync();

        public Task<List<int>> AttendeeUserIdsAsync(int entryId) =>
            _context.Set<ArchiveAttendance>()
                .Where(a => a.ArchiveEntryId == entryId)
                .Select(a => a.UserId)
                .ToListAsync();

        public async Task<(List<User> Users, int TotalCount)> PageAttendeesAsync(int entryId, int skip, int take)
        {
            var query = _context.Set<ArchiveAttendance>()
                .AsNoTracking()
                .Where(a => a.ArchiveEntryId == entryId && a.User!.IsActive);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(a => a.CreatedAt)
                .ThenBy(a => a.UserId)
                .Skip(skip)
                .Take(take)
                .Select(a => a.User!)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<(List<User> Users, int TotalCount)> PageLikersAsync(int entryId, int skip, int take)
        {
            var query = _context.Set<ArchiveLike>()
                .AsNoTracking()
                .Where(l => l.ArchiveEntryId == entryId && l.User!.IsActive);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(l => l.CreatedAt)
                .ThenBy(l => l.UserId)
                .Skip(skip)
                .Take(take)
                .Select(l => l.User!)
                .ToListAsync();

            return (users, totalCount);
        }

        private async Task<bool> AddLinkAsync<TLink>(TLink link, System.Linq.Expressions.Expression<Func<TLink, bool>> exists) where TLink : class
        {
            var set = _context.Set<TLink>();

            if (await set.AnyAsync(exists))
                return false;

            set.Add(link);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.Entry(link).State = EntityState.Detached;

                if (await set.AsNoTracking().AnyAsync(exists))
                    return false;

                throw;
            }
        }

        private IQueryable<ArchiveEntry> Visible() =>
            Existing().Where(e => e.Status == EArchiveEntryStatus.Published);

        private IQueryable<ArchiveEntry> Existing() =>
            _dbSet.AsNoTracking().Where(e => !e.IsDeleted && e.Author!.IsActive);

        private static IQueryable<ArchiveReadModel> Project(IQueryable<ArchiveEntry> query, int viewerUserId) =>
            query.Select(e => new ArchiveReadModel
            {
                Id = e.Id,
                AuthorUserId = e.AuthorUserId,
                Status = e.Status,
                IsSubmission = e.IsSubmission,
                RejectionReason = e.RejectionReason,
                ReviewedAt = e.ReviewedAt,
                AuthorName = e.Author!.Name,
                AuthorNickname = e.Author.Nickname,
                AuthorAvatarKey = e.Author.AvatarKey,
                Title = e.Title,
                Description = e.Description,
                Year = e.Year,
                Month = e.Month,
                Day = e.Day,
                Venue = e.Venue,
                City = e.City == null ? null : new CityDto { Id = e.City.Id, Name = e.City.Name, Uf = e.City.Uf },
                CityName = e.CityName,
                CoverMediaUploadId = e.CoverMediaUploadId,
                CreatedAt = e.CreatedAt,
                Bands = e.Bands
                    .Where(b => !b.IsDeleted)
                    .OrderBy(b => b.Order)
                    .Select(b => new ArchiveBandRow { BandId = b.BandId, Name = b.Name })
                    .ToList(),
                PhotoCount = e.Media.Count(m => !m.IsDeleted && m.MediaUpload!.Kind == EMediaKind.Photo),
                VideoCount = e.Media.Count(m => !m.IsDeleted && m.MediaUpload!.Kind == EMediaKind.Video),
                LikeCount = e.Likes.Count,
                LikedByMe = e.Likes.Any(l => l.UserId == viewerUserId),
                AttendanceCount = e.Attendances.Count,
                AttendedByMe = e.Attendances.Any(a => a.UserId == viewerUserId),
                CommentCount = e.Comments.Count(c => !c.IsDeleted && c.Author!.IsActive),
            });
    }
}
