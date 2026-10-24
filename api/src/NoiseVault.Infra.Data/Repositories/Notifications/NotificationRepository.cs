using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Notifications.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GroupAsync(IReadOnlyCollection<int> userIds, NotificationInput input, DateTime now)
        {
            var kind = input.Kind;
            var actorUserId = input.ActorUserId;
            var postId = input.PostId;
            var eventId = input.EventId;
            var archiveEntryId = input.ArchiveEntryId;
            var supportTicketId = input.SupportTicketId;

            var query = _context.Notifications.Where(n => userIds.Contains(n.UserId)
                && n.Kind == kind
                && n.ReadAt == null
                && n.PostId == postId
                && n.EventId == eventId
                && n.ArchiveEntryId == archiveEntryId
                && n.SupportTicketId == supportTicketId);

            var grouped = await query.Select(n => n.UserId).Distinct().ToListAsync();

            if (grouped.Count == 0)
                return grouped;

            await query.ExecuteUpdateAsync(s => s
                .SetProperty(n => n.ActorCount, n => n.ActorUserId == actorUserId ? n.ActorCount : n.ActorCount + 1)
                .SetProperty(n => n.ActorUserId, actorUserId)
                .SetProperty(n => n.LastActivityAt, now));

            return grouped;
        }

        public async Task AddRangeAsync(IReadOnlyCollection<Notification> notifications)
        {
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<NotificationReadModel> Items, int TotalCount)> PageAsync(int userId, bool unreadOnly, int skip, int take)
        {
            var query = Visible(userId);

            if (unreadOnly)
                query = query.Where(n => n.ReadAt == null);

            var totalCount = await query.CountAsync();

            var items = await Project(query
                    .OrderByDescending(n => n.LastActivityAt)
                    .ThenByDescending(n => n.Id)
                    .Skip(skip)
                    .Take(take))
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Dictionary<int, int>> UnreadCountsAsync(IReadOnlyCollection<int> userIds)
        {
            var rows = await VisibleFor(_context.Notifications.AsNoTracking().Where(n => userIds.Contains(n.UserId) && n.ReadAt == null))
                .GroupBy(n => n.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(r => r.UserId, r => r.Count);
        }

        public async Task<bool> MarkReadAsync(int userId, int notificationId, DateTime now)
        {
            var target = Visible(userId).Where(n => n.Id == notificationId);

            if (!await target.AnyAsync())
                return false;

            await _context.Notifications
                .Where(n => n.Id == notificationId && n.UserId == userId && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));

            return true;
        }

        public Task<int> MarkAllReadAsync(int userId, DateTime now) =>
            _context.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));

        public Task<int> MarkInvitationReadAsync(int userId, int invitationId, DateTime now) =>
            _context.Notifications
                .Where(n => n.UserId == userId && n.BandInvitationId == invitationId && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));

        public async Task<bool> DeleteAsync(int userId, int notificationId) =>
            await _context.Notifications
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteDeleteAsync() > 0;

        public Task<int> PurgeAsync(DateTime olderThan) =>
            _context.Notifications
                .Where(n => n.LastActivityAt < olderThan
                    || (n.PostId != null && (n.Post!.IsDeleted || n.Post.PublishedAt == null))
                    || (n.EventId != null && n.Event!.IsDeleted)
                    || (n.ArchiveEntryId != null && n.ArchiveEntry!.IsDeleted)
                    || (n.BandId != null && n.Band!.IsDeleted)
                    || (n.BandInvitationId != null && n.BandInvitation!.Status == EBandInvitationStatus.Revoked)
                    || (n.SupportTicketId != null && n.SupportTicket!.IsDeleted)
                    || (n.BandOpeningId != null && n.BandOpening!.IsDeleted))
                .ExecuteDeleteAsync();

        private IQueryable<Notification> Visible(int userId) =>
            VisibleFor(_context.Notifications.AsNoTracking().Where(n => n.UserId == userId));

        private static IQueryable<Notification> VisibleFor(IQueryable<Notification> query) =>
            query.Where(n =>
                (n.PostId == null || (!n.Post!.IsDeleted && n.Post.PublishedAt != null))
                && (n.EventId == null || !n.Event!.IsDeleted)
                && (n.ArchiveEntryId == null || !n.ArchiveEntry!.IsDeleted)
                && (n.BandId == null || !n.Band!.IsDeleted)
                && (n.BandInvitationId == null || n.BandInvitation!.Status != EBandInvitationStatus.Revoked)
                && (n.SupportTicketId == null || !n.SupportTicket!.IsDeleted)
                && (n.BandOpeningId == null || !n.BandOpening!.IsDeleted));

        private static IQueryable<NotificationReadModel> Project(IQueryable<Notification> query) =>
            query.Select(n => new NotificationReadModel
            {
                Id = n.Id,
                Kind = n.Kind,
                ActorCount = n.ActorCount,
                LastActivityAt = n.LastActivityAt,
                ReadAt = n.ReadAt,
                ActorId = n.ActorUserId,
                ActorName = n.Actor != null ? n.Actor.Name : null,
                ActorNickname = n.Actor != null ? n.Actor.Nickname : null,
                ActorAvatarKey = n.Actor != null ? n.Actor.AvatarKey : null,
                PostId = n.PostId,
                PostContent = n.Post != null ? n.Post.Content : null,
                EventId = n.EventId,
                EventTitle = n.Event != null ? n.Event.Title : null,
                EventStartsAt = n.Event != null ? n.Event.StartsAt : null,
                ArchiveEntryId = n.ArchiveEntryId,
                ArchiveTitle = n.ArchiveEntry != null ? n.ArchiveEntry.Title : null,
                BandId = n.BandId,
                BandName = n.Band != null ? n.Band.Name : null,
                BandSlug = n.Band != null ? n.Band.Slug : null,
                BandAvatarKey = n.Band != null ? n.Band.AvatarKey : null,
                BandIsVerified = n.Band != null && n.Band.IsVerified,
                BandKind = n.Band != null ? n.Band.Kind : null,
                BandGenres = n.Band != null ? n.Band.Genres.Select(g => g.Genre).ToList() : new(),
                InvitationId = n.BandInvitationId,
                InvitationStatus = n.BandInvitation != null ? n.BandInvitation.Status : null,
                InvitationInstrument = n.BandInvitation != null ? n.BandInvitation.Instrument : null,
                InvitationExpiresAt = n.BandInvitation != null ? n.BandInvitation.ExpiresAt : null,
                TicketId = n.SupportTicketId,
                TicketTitle = n.SupportTicket != null ? n.SupportTicket.Title : null,
                TicketStatus = n.SupportTicket != null ? n.SupportTicket.Status : null,
                TicketMine = n.SupportTicket != null && n.SupportTicket.ReporterUserId == n.UserId,
                OpeningId = n.BandOpeningId,
                OpeningInstrument = n.BandOpening != null ? n.BandOpening.Instrument : null,
                OpeningOtherName = n.BandOpening != null ? n.BandOpening.OtherName : null,
            });
    }
}
