using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Support;
using NoiseVault.Application.Support.Interfaces;
using NoiseVault.Application.Support.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Support;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.Support
{
    public class SupportTicketRepository : Repository<SupportTicket>, ISupportTicketRepository
    {
        public SupportTicketRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(List<SupportTicketReadModel> Items, int TotalCount)> PageAsync(SupportTicketFilter filter, int skip, int take)
        {
            var query = Visible();

            if (filter.ReporterUserId != null)
                query = query.Where(t => t.ReporterUserId == filter.ReporterUserId);

            if (filter.Status != null)
                query = query.Where(t => t.Status == filter.Status);

            if (filter.Tag != null)
                query = query.Where(t => t.Tags.Any(x => x.Tag == filter.Tag));

            if (filter.Area != null)
                query = query.Where(t => t.Areas.Any(x => x.Area == filter.Area));

            if (filter.Priority != null)
                query = query.Where(t => t.Priority == filter.Priority);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                var protocolId = filter.ProtocolId;

                query = query.Where(t => (protocolId != null && t.Id == protocolId)
                    || t.Title.ToLower().Contains(term)
                    || t.Description.ToLower().Contains(term)
                    || t.Reporter!.Name.ToLower().Contains(term)
                    || t.Reporter.Nickname.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var ordered = filter.Sort switch
            {
                ESupportTicketSort.Newest => query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id),
                ESupportTicketSort.Priority => query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.LastActivityAt).ThenByDescending(t => t.Id),
                _ => query.OrderByDescending(t => t.LastActivityAt).ThenByDescending(t => t.Id),
            };

            var items = await Project(ordered.Skip(skip).Take(take)).ToListAsync();

            return (items, totalCount);
        }

        public Task<SupportTicketReadModel?> FindAsync(int ticketId, int? reporterUserId)
        {
            var query = Visible().Where(t => t.Id == ticketId);

            if (reporterUserId != null)
                query = query.Where(t => t.ReporterUserId == reporterUserId);

            return Project(query).FirstOrDefaultAsync();
        }

        public async Task<Dictionary<ESupportTicketStatus, int>> CountByStatusAsync()
        {
            var rows = await Visible()
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(r => r.Status, r => r.Count);
        }

        public Task<int> CountOpenAsync(int reporterUserId) =>
            Visible().CountAsync(t => t.ReporterUserId == reporterUserId
                && (t.Status == ESupportTicketStatus.Open || t.Status == ESupportTicketStatus.InProgress));

        public async Task<(List<SupportTicketMessage> Items, int TotalCount)> PageMessagesAsync(int ticketId, int skip, int take)
        {
            var query = _context.Set<SupportTicketMessage>()
                .AsNoTracking()
                .Where(m => m.SupportTicketId == ticketId && !m.IsDeleted);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id)
                .Skip(skip)
                .Take(take)
                .Include(m => m.Author)
                .ToListAsync();

            return (items, totalCount);
        }

        private IQueryable<SupportTicket> Visible() =>
            _dbSet.AsNoTracking().Where(t => !t.IsDeleted);

        private static IQueryable<SupportTicketReadModel> Project(IQueryable<SupportTicket> query) =>
            query.Select(t => new SupportTicketReadModel
            {
                Id = t.Id,
                ReporterUserId = t.ReporterUserId,
                ReporterName = t.Reporter!.Name,
                ReporterNickname = t.Reporter.Nickname,
                ReporterAvatarKey = t.Reporter.AvatarKey,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                Tags = t.Tags.Select(x => x.Tag).ToList(),
                Areas = t.Areas.Select(x => x.Area).ToList(),
                PhotoCount = t.Media.Count(m => !m.IsDeleted && m.MediaUpload!.Kind == EMediaKind.Photo),
                VideoCount = t.Media.Count(m => !m.IsDeleted && m.MediaUpload!.Kind == EMediaKind.Video),
                MessageCount = t.Messages.Count(m => !m.IsDeleted),
                LastMessageFromStaff = t.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .ThenByDescending(m => m.Id)
                    .Select(m => (bool?)m.FromStaff)
                    .FirstOrDefault(),
                OriginPath = t.OriginPath,
                UserAgent = t.UserAgent,
                ViewportWidth = t.ViewportWidth,
                ViewportHeight = t.ViewportHeight,
                InternalNote = t.InternalNote,
                CreatedAt = t.CreatedAt,
                LastActivityAt = t.LastActivityAt,
                ClosedAt = t.ClosedAt,
            });
    }
}
