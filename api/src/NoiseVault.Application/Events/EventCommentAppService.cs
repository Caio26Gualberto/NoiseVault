using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Events;

namespace NoiseVault.Application.Events
{
    public class EventCommentAppService : IEventCommentAppService
    {
        private readonly IRepository<EventComment> _commentRepository;
        private readonly IEventAppService _eventAppService;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly INotificationAppService _notificationAppService;
        private readonly IApplicationLogger _logger;

        public EventCommentAppService(
            IRepository<EventComment> commentRepository,
            IEventAppService eventAppService,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            INotificationAppService notificationAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _commentRepository = commentRepository;
            _eventAppService = eventAppService;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Events);
        }

        public async Task<Result<PagedResultDto<EventCommentDto>>> List(int eventId, PagedRequestDto query)
        {
            query.Normalize();

            var row = await _eventAppService.FindVisible(eventId);

            if (row == null)
                return Result<PagedResultDto<EventCommentDto>>.Fail(EventNotFound());

            var options = new QueryOptions<EventComment>
            {
                Predicate = c => c.EventId == eventId && c.Author!.IsActive,
                Includes = [c => c.Author!],
                OrderBy = q => q.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _commentRepository.CountAsync(options);
            var comments = await _commentRepository.ListAsync(options);
            var canModerate = row.OrganizerUserId == _currentUser.UserId || row.ViewerOwnsBand;

            return Result<PagedResultDto<EventCommentDto>>.Ok(
                PagedResultDto<EventCommentDto>.Create(comments.Select(c => Map(c, canModerate)).ToList(), query.Page, query.PageSize, totalCount),
                "EVENT_COMMENTS_FOUND",
                "Comments found");
        }

        public async Task<Result<EventCommentDto>> Add(int eventId, CommentInputDto input)
        {
            _logger.SetOperation("AddEventComment");

            var text = PostRules.NormalizeText(input.Text);

            if (text.Length is 0 or > PostRules.CommentMaxLength)
                return Result<EventCommentDto>.Fail(new Error("INVALID_COMMENT_TEXT", $"Comment must have between 1 and {PostRules.CommentMaxLength} characters", ErrorType.Validation));

            var row = await _eventAppService.FindVisible(eventId);

            if (row == null)
                return Result<EventCommentDto>.Fail(EventNotFound());

            if (row.Status == EEventStatus.Cancelled)
                return Result<EventCommentDto>.Fail(new Error("EVENT_CANCELLED", "This event was cancelled", ErrorType.Conflict));

            var comment = new EventComment { EventId = eventId, AuthorUserId = _currentUser.UserId, Text = text };
            await _commentRepository.AddAsync(comment);

            await _notificationAppService.Notify(row.OrganizerUserId, new NotificationInput(ENotificationKind.EventCommented, comment.AuthorUserId, EventId: eventId));

            var saved = await _commentRepository.FirstOrDefaultAsync(new QueryOptions<EventComment>
            {
                Predicate = c => c.Id == comment.Id,
                Includes = [c => c.Author!],
                AsNoTracking = true,
            });

            return Result<EventCommentDto>.Ok(
                Map(saved!, row.OrganizerUserId == _currentUser.UserId || row.ViewerOwnsBand),
                "EVENT_COMMENT_CREATED",
                "Comment added");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteEventComment");

            var me = _currentUser.UserId;

            var comment = await _commentRepository.FirstOrDefaultAsync(new QueryOptions<EventComment>
            {
                Predicate = c => c.Id == id && !c.Event!.IsDeleted,
                Includes = [c => c.Event!],
            });

            if (comment == null)
                return Result.Fail(new Error("EVENT_COMMENT_NOT_FOUND", "Comment not found", ErrorType.NotFound));

            var row = await _eventAppService.FindVisible(comment.EventId);
            var canModerate = row != null && (row.OrganizerUserId == me || row.ViewerOwnsBand);

            if (comment.AuthorUserId != me && !canModerate)
                return Result.Fail(new Error("EVENT_COMMENT_FORBIDDEN", "Only the comment author or the organizer can delete it", ErrorType.Forbidden));

            await _commentRepository.SoftDelete(comment);

            _logger.Info("EVENT_COMMENT_DELETED", "Event comment deleted", userId: me, extra: new { CommentId = id, comment.EventId, ByOrganizer = comment.AuthorUserId != me });

            return Result.Ok("EVENT_COMMENT_DELETED", "Comment deleted");
        }

        private EventCommentDto Map(EventComment comment, bool canModerate) => new()
        {
            Id = comment.Id,
            Author = new ProfileSummaryDto
            {
                Id = comment.Author!.Id,
                Name = comment.Author.Name,
                Nickname = comment.Author.Nickname,
                AvatarUrl = _fileStorage.PublicUrl(comment.Author.AvatarKey),
            },
            Text = comment.Text,
            CreatedAt = comment.CreatedAt,
            CanDelete = comment.AuthorUserId == _currentUser.UserId || canModerate,
        };

        private static Error EventNotFound() => new("EVENT_NOT_FOUND", "Event not found", ErrorType.NotFound);
    }
}
