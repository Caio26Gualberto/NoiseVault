using NoiseVault.Application.Cities.Dtos;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Events.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Events;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.Events
{
    public class EventRepository : Repository<Event>, IEventRepository
    {
        public EventRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(List<EventReadModel> Items, int TotalCount)> PageAsync(EventFilter filter, int viewerUserId, int skip, int take)
        {
            var query = Visible();

            query = filter.Past
                ? query.Where(e => e.EndsAtOrDefault < filter.Now)
                : query.Where(e => e.EndsAtOrDefault >= filter.Now);

            if (filter.Genre != null)
                query = query.Where(e => e.Genres.Any(g => g.Genre == filter.Genre));

            if (filter.CityId != null)
                query = query.Where(e => e.CityId == filter.CityId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(term)
                    || e.Venue.ToLower().Contains(term)
                    || (e.CityName != null && e.CityName.ToLower().Contains(term))
                    || (e.City != null && e.City.Name.ToLower().Contains(term))
                    || e.Lineup.Any(l => !l.IsDeleted
                        && ((l.Name != null && l.Name.ToLower().Contains(term))
                            || (l.Band != null && l.Band.Name.ToLower().Contains(term)))));
            }

            if (filter.InterestedOnly)
                query = query.Where(e => e.Interests.Any(i => i.UserId == viewerUserId));

            if (filter.BandId != null)
                query = query.Where(e => e.BandId == filter.BandId || e.Lineup.Any(l => !l.IsDeleted && l.BandId == filter.BandId));

            if (filter.OrganizerUserId != null)
                query = query.Where(e => e.OrganizerUserId == filter.OrganizerUserId);

            var totalCount = await query.CountAsync();

            var ordered = filter.Past
                ? query.OrderByDescending(e => e.StartsAt).ThenByDescending(e => e.Id)
                : query.OrderBy(e => e.StartsAt).ThenBy(e => e.Id);

            var items = await Project(ordered.Skip(skip).Take(take), viewerUserId).ToListAsync();

            return (items, totalCount);
        }

        public Task<EventReadModel?> FindAsync(int eventId, int viewerUserId) =>
            Project(Visible().Where(e => e.Id == eventId), viewerUserId).FirstOrDefaultAsync();

        public Task<List<EventReadModel>> ListByIdsAsync(IReadOnlyCollection<int> eventIds, int viewerUserId) =>
            Project(Visible().Where(e => eventIds.Contains(e.Id)), viewerUserId).ToListAsync();

        public async Task<bool> AddInterestAsync(int eventId, int userId)
        {
            var interests = _context.Set<EventInterest>();

            if (await interests.AnyAsync(i => i.EventId == eventId && i.UserId == userId))
                return false;

            var interest = new EventInterest { EventId = eventId, UserId = userId };
            interests.Add(interest);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.Entry(interest).State = EntityState.Detached;

                if (await interests.AsNoTracking().AnyAsync(i => i.EventId == eventId && i.UserId == userId))
                    return false;

                throw;
            }
        }

        public Task RemoveInterestAsync(int eventId, int userId) =>
            _context.Set<EventInterest>()
                .Where(i => i.EventId == eventId && i.UserId == userId)
                .ExecuteDeleteAsync();

        public async Task<bool> TryCancelAsync(int eventId, DateTime now)
        {
            var affected = await _dbSet
                .Where(e => e.Id == eventId && e.Status == EEventStatus.Published && !e.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, EEventStatus.Cancelled)
                    .SetProperty(e => e.CancelledAt, now)
                    .SetProperty(e => e.UpdatedAt, now));

            return affected == 1;
        }

        public async Task<(List<User> Users, int TotalCount)> PageInterestedAsync(int eventId, int skip, int take)
        {
            var query = _context.Set<EventInterest>()
                .AsNoTracking()
                .Where(i => i.EventId == eventId && i.User!.IsActive);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(i => i.CreatedAt)
                .ThenBy(i => i.UserId)
                .Skip(skip)
                .Take(take)
                .Select(i => i.User!)
                .ToListAsync();

            return (users, totalCount);
        }

        public Task<List<int>> InterestedUserIdsAsync(int eventId) =>
            _context.Set<EventInterest>()
                .Where(i => i.EventId == eventId)
                .Select(i => i.UserId)
                .ToListAsync();

        public async Task<List<(int EventId, List<int> UserIds)>> InterestedStartingBetweenAsync(DateTime fromUtc, DateTime toUtc)
        {
            var rows = await _dbSet
                .Where(e => e.StartsAt >= fromUtc && e.StartsAt < toUtc && e.Status == EEventStatus.Published && !e.IsDeleted)
                .SelectMany(e => e.Interests.Select(i => new { EventId = e.Id, i.UserId }))
                .ToListAsync();

            return rows
                .GroupBy(r => r.EventId)
                .Select(g => (g.Key, g.Select(r => r.UserId).ToList()))
                .ToList();
        }

        public Task<int> CountUpcomingByBandAsync(int bandId, DateTime now) =>
            Visible().CountAsync(e => e.Status == EEventStatus.Published
                && e.EndsAtOrDefault >= now
                && (e.BandId == bandId || e.Lineup.Any(l => !l.IsDeleted && l.BandId == bandId)));

        public Task<int> CountUpcomingByOrganizerAsync(int userId, DateTime now) =>
            _dbSet.CountAsync(e => !e.IsDeleted
                && e.OrganizerUserId == userId
                && e.Status == EEventStatus.Published
                && e.EndsAtOrDefault >= now);

        private IQueryable<Event> Visible() =>
            _dbSet.AsNoTracking().Where(e => !e.IsDeleted && e.Organizer!.IsActive);

        private static IQueryable<EventReadModel> Project(IQueryable<Event> query, int viewerUserId) =>
            query.Select(e => new EventReadModel
            {
                Id = e.Id,
                OrganizerUserId = e.OrganizerUserId,
                OrganizerName = e.Organizer!.Name,
                OrganizerNickname = e.Organizer.Nickname,
                OrganizerAvatarKey = e.Organizer.AvatarKey,
                BandId = e.BandId,
                Title = e.Title,
                Description = e.Description,
                StartsAt = e.StartsAt,
                EndsAt = e.EndsAt,
                EndsAtOrDefault = e.EndsAtOrDefault,
                Venue = e.Venue,
                City = e.City == null ? null : new CityDto { Id = e.City.Id, Name = e.City.Name, Uf = e.City.Uf },
                CityName = e.CityName,
                TicketsUrl = e.TicketsUrl,
                Price = e.Price,
                Capacity = e.Capacity,
                BannerKey = e.BannerKey,
                BannerPosition = e.BannerPosition,
                Status = e.Status,
                CancelledAt = e.CancelledAt,
                CreatedAt = e.CreatedAt,
                Genres = e.Genres.OrderBy(g => g.Genre).Select(g => g.Genre).ToList(),
                Lineup = e.Lineup
                    .Where(l => !l.IsDeleted)
                    .OrderBy(l => l.Order)
                    .Select(l => new EventLineupRow { BandId = l.BandId, Name = l.Name, Order = l.Order })
                    .ToList(),
                InterestedCount = e.Interests.Count,
                Interested = e.Interests.Any(i => i.UserId == viewerUserId),
                CommentCount = e.Comments.Count(c => !c.IsDeleted && c.Author!.IsActive),
                ViewerOwnsBand = e.BandId != null
                    && e.Band!.Members.Any(m => m.UserId == viewerUserId && m.Role == EBandMemberRole.Owner),
            });
    }
}
