using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Archive.Models;
using NoiseVault.Domain.Entities;
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

            if (filter.City != null)
                query = query.Where(e => e.City == filter.City);

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

        public Task<ArchiveReadModel?> FindAsync(int entryId, int viewerUserId) =>
            Project(Visible().Where(e => e.Id == entryId), viewerUserId).FirstOrDefaultAsync();

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
            _dbSet.AsNoTracking().Where(e => !e.IsDeleted && e.Author!.IsActive);

        private static IQueryable<ArchiveReadModel> Project(IQueryable<ArchiveEntry> query, int viewerUserId) =>
            query.Select(e => new ArchiveReadModel
            {
                Id = e.Id,
                AuthorUserId = e.AuthorUserId,
                AuthorName = e.Author!.Name,
                AuthorNickname = e.Author.Nickname,
                AuthorAvatarKey = e.Author.AvatarKey,
                Title = e.Title,
                Description = e.Description,
                Year = e.Year,
                Month = e.Month,
                Day = e.Day,
                Venue = e.Venue,
                City = e.City,
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
