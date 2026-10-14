using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Realtime;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Notifications.Dtos;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Notifications.Models;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.Notifications
{
    public class NotificationAppService : INotificationAppService
    {
        private const int ExcerptLength = 80;

        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationPusher _pusher;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly IApplicationLogger _logger;

        public NotificationAppService(
            INotificationRepository notificationRepository,
            INotificationPusher pusher,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            IApplicationLoggerFactory loggerFactory)
        {
            _notificationRepository = notificationRepository;
            _pusher = pusher;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _logger = loggerFactory.Create(LogContext.Notifications);
        }

        public async Task<Result<PagedResultDto<NotificationDto>>> List(NotificationListQueryDto query)
        {
            query.Normalize();

            var (rows, totalCount) = await _notificationRepository.PageAsync(_currentUser.UserId, query.UnreadOnly, query.Skip, query.PageSize);

            return Result<PagedResultDto<NotificationDto>>.Ok(
                PagedResultDto<NotificationDto>.Create(rows.Select(Map).ToList(), query.Page, query.PageSize, totalCount),
                "NOTIFICATIONS_FOUND",
                "Notifications found");
        }

        public async Task<Result<UnreadCountDto>> UnreadCount()
        {
            var me = _currentUser.UserId;
            var counts = await _notificationRepository.UnreadCountsAsync([me]);

            return Result<UnreadCountDto>.Ok(
                new UnreadCountDto { Count = counts.GetValueOrDefault(me) },
                "NOTIFICATIONS_UNREAD_COUNT_FOUND",
                "Unread count found");
        }

        public async Task<Result> MarkRead(int id)
        {
            var me = _currentUser.UserId;

            if (!await _notificationRepository.MarkReadAsync(me, id, DateTime.UtcNow))
                return Result.Fail(NotFound());

            await PushAsync([me]);

            return Result.Ok("NOTIFICATION_READ", "Notification marked as read");
        }

        public async Task<Result> MarkAllRead()
        {
            var me = _currentUser.UserId;

            await _notificationRepository.MarkAllReadAsync(me, DateTime.UtcNow);
            await PushAsync([me]);

            return Result.Ok("NOTIFICATIONS_ALL_READ", "All notifications marked as read");
        }

        public async Task<Result> Delete(int id)
        {
            var me = _currentUser.UserId;

            if (!await _notificationRepository.DeleteAsync(me, id))
                return Result.Fail(NotFound());

            await PushAsync([me]);

            return Result.Ok("NOTIFICATION_DELETED", "Notification deleted");
        }

        public Task Notify(int recipientUserId, NotificationInput input) =>
            NotifyMany([recipientUserId], input);

        public async Task NotifyMany(IReadOnlyCollection<int> recipientUserIds, NotificationInput input)
        {
            var recipients = recipientUserIds.Distinct().Where(id => id != input.ActorUserId).ToList();

            if (recipients.Count == 0)
                return;

            try
            {
                var now = DateTime.UtcNow;

                var grouped = NotificationRules.IsGroupable(input.Kind)
                    ? await _notificationRepository.GroupAsync(recipients, input, now)
                    : [];

                var fresh = recipients
                    .Except(grouped)
                    .Select(userId => new Notification
                    {
                        UserId = userId,
                        Kind = input.Kind,
                        ActorUserId = input.ActorUserId,
                        PostId = input.PostId,
                        EventId = input.EventId,
                        ArchiveEntryId = input.ArchiveEntryId,
                        BandId = input.BandId,
                        BandInvitationId = input.BandInvitationId,
                        CreatedAt = now,
                        LastActivityAt = now,
                    })
                    .ToList();

                if (fresh.Count > 0)
                    await _notificationRepository.AddRangeAsync(fresh);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "NOTIFICATION_FAILED",
                    "Notification could not be stored",
                    exception: ex,
                    userId: input.ActorUserId,
                    extra: new { input.Kind, Recipients = recipients.Count, input.PostId, input.EventId, input.ArchiveEntryId, input.BandId, input.BandInvitationId });
                return;
            }

            await PushAsync(recipients);
        }

        public async Task MarkInvitationRead(int userId, int invitationId)
        {
            if (await _notificationRepository.MarkInvitationReadAsync(userId, invitationId, DateTime.UtcNow) > 0)
                await PushAsync([userId]);
        }

        public async Task<int> Purge()
        {
            var removed = await _notificationRepository.PurgeAsync(DateTime.UtcNow.AddDays(-NotificationRules.RetentionDays));

            _logger.Info("NOTIFICATIONS_PURGED", "Old notifications purged", extra: new { Removed = removed, NotificationRules.RetentionDays });

            return removed;
        }

        private async Task PushAsync(IReadOnlyCollection<int> userIds)
        {
            try
            {
                var counts = await _notificationRepository.UnreadCountsAsync(userIds);
                var unreadByUser = userIds.ToDictionary(id => id, id => counts.GetValueOrDefault(id));

                await _pusher.UnreadChanged(unreadByUser);
            }
            catch (Exception ex)
            {
                _logger.Error("NOTIFICATION_PUSH_FAILED", "Unread count could not be pushed", exception: ex, extra: new { Recipients = userIds.Count });
            }
        }

        private NotificationDto Map(NotificationReadModel row) => new()
        {
            Id = row.Id,
            Kind = row.Kind,
            ActorCount = row.ActorCount,
            ReadAt = row.ReadAt,
            LastActivityAt = row.LastActivityAt,
            Actor = row.ActorId == null
                ? null
                : new ProfileSummaryDto
                {
                    Id = row.ActorId.Value,
                    Name = row.ActorName ?? string.Empty,
                    Nickname = row.ActorNickname ?? string.Empty,
                    AvatarUrl = _fileStorage.PublicUrl(row.ActorAvatarKey),
                },
            Post = row.PostId == null
                ? null
                : new NotificationPostDto { Id = row.PostId.Value, Excerpt = Excerpt(row.PostContent) },
            Event = row.EventId == null
                ? null
                : new NotificationEventDto { Id = row.EventId.Value, Title = row.EventTitle ?? string.Empty, StartsAt = row.EventStartsAt ?? default },
            Archive = row.ArchiveEntryId == null
                ? null
                : new NotificationArchiveDto { Id = row.ArchiveEntryId.Value, Title = row.ArchiveTitle ?? string.Empty },
            Band = row.BandId == null
                ? null
                : new BandSummaryDto
                {
                    Id = row.BandId.Value,
                    Name = row.BandName ?? string.Empty,
                    Slug = row.BandSlug ?? string.Empty,
                    AvatarUrl = _fileStorage.PublicUrl(row.BandAvatarKey),
                    IsVerified = row.BandIsVerified,
                    Genres = row.BandGenres,
                },
            Invitation = row.InvitationId == null
                ? null
                : new NotificationInvitationDto
                {
                    Id = row.InvitationId.Value,
                    Status = row.InvitationStatus!.Value,
                    Instrument = row.InvitationInstrument,
                    ExpiresAt = row.InvitationExpiresAt ?? default,
                },
        };

        private static string Excerpt(string? content)
        {
            var text = (content ?? string.Empty).ReplaceLineEndings(" ").Trim();
            return text.Length <= ExcerptLength ? text : text[..ExcerptLength].TrimEnd() + "…";
        }

        private static Error NotFound() =>
            new("NOTIFICATION_NOT_FOUND", "Notification not found", ErrorType.NotFound);
    }
}
