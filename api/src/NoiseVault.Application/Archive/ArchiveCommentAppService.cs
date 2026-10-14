using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Archive
{
    public class ArchiveCommentAppService : IArchiveCommentAppService
    {
        private readonly IRepository<ArchiveComment> _commentRepository;
        private readonly IArchiveAppService _archiveAppService;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IFileStorage _fileStorage;
        private readonly INotificationAppService _notificationAppService;
        private readonly IApplicationLogger _logger;

        public ArchiveCommentAppService(
            IRepository<ArchiveComment> commentRepository,
            IArchiveAppService archiveAppService,
            ICurrentUser currentUser,
            IPermissionChecker permissionChecker,
            IFileStorage fileStorage,
            INotificationAppService notificationAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _commentRepository = commentRepository;
            _archiveAppService = archiveAppService;
            _currentUser = currentUser;
            _permissionChecker = permissionChecker;
            _fileStorage = fileStorage;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Archive);
        }

        public async Task<Result<PagedResultDto<ArchiveCommentDto>>> List(int entryId, PagedRequestDto query)
        {
            query.Normalize();

            if (await _archiveAppService.FindVisible(entryId) == null)
                return Result<PagedResultDto<ArchiveCommentDto>>.Fail(EntryNotFound());

            var options = new QueryOptions<ArchiveComment>
            {
                Predicate = c => c.ArchiveEntryId == entryId && c.Author!.IsActive,
                Includes = [c => c.Author!],
                OrderBy = q => q.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _commentRepository.CountAsync(options);
            var comments = await _commentRepository.ListAsync(options);
            var canModerate = await CanModerateAsync();

            return Result<PagedResultDto<ArchiveCommentDto>>.Ok(
                PagedResultDto<ArchiveCommentDto>.Create(comments.Select(c => Map(c, canModerate)).ToList(), query.Page, query.PageSize, totalCount),
                "ARCHIVE_COMMENTS_FOUND",
                "Comments found");
        }

        public async Task<Result<ArchiveCommentDto>> Add(int entryId, CommentInputDto input)
        {
            _logger.SetOperation("AddArchiveComment");

            var text = PostRules.NormalizeText(input.Text);

            if (text.Length is 0 or > PostRules.CommentMaxLength)
                return Result<ArchiveCommentDto>.Fail(new Error("INVALID_COMMENT_TEXT", $"Comment must have between 1 and {PostRules.CommentMaxLength} characters", ErrorType.Validation));

            var entry = await _archiveAppService.FindVisible(entryId);

            if (entry == null)
                return Result<ArchiveCommentDto>.Fail(EntryNotFound());

            var comment = new ArchiveComment { ArchiveEntryId = entryId, AuthorUserId = _currentUser.UserId, Text = text };
            await _commentRepository.AddAsync(comment);

            await _notificationAppService.Notify(entry.AuthorUserId, new NotificationInput(ENotificationKind.ArchiveCommented, comment.AuthorUserId, ArchiveEntryId: entryId));

            var attendees = (await _archiveAppService.AttendeeUserIds(entryId)).Where(id => id != entry.AuthorUserId).ToList();
            await _notificationAppService.NotifyMany(attendees, new NotificationInput(ENotificationKind.ArchiveCommentedWhereAttended, comment.AuthorUserId, ArchiveEntryId: entryId));

            var saved = await _commentRepository.FirstOrDefaultAsync(new QueryOptions<ArchiveComment>
            {
                Predicate = c => c.Id == comment.Id,
                Includes = [c => c.Author!],
                AsNoTracking = true,
            });

            return Result<ArchiveCommentDto>.Ok(Map(saved!, await CanModerateAsync()), "ARCHIVE_COMMENT_CREATED", "Comment added");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteArchiveComment");

            var me = _currentUser.UserId;

            var comment = await _commentRepository.FirstOrDefaultAsync(new QueryOptions<ArchiveComment>
            {
                Predicate = c => c.Id == id && !c.ArchiveEntry!.IsDeleted,
            });

            if (comment == null)
                return Result.Fail(new Error("ARCHIVE_COMMENT_NOT_FOUND", "Comment not found", ErrorType.NotFound));

            if (comment.AuthorUserId != me && !await CanModerateAsync())
                return Result.Fail(new Error("ARCHIVE_COMMENT_FORBIDDEN", "Only the comment author or a curator can delete it", ErrorType.Forbidden));

            await _commentRepository.SoftDelete(comment);

            _logger.Info("ARCHIVE_COMMENT_DELETED", "Archive comment deleted", userId: me, extra: new { CommentId = id, comment.ArchiveEntryId, ByCurator = comment.AuthorUserId != me });

            return Result.Ok("ARCHIVE_COMMENT_DELETED", "Comment deleted");
        }

        private Task<bool> CanModerateAsync() => _permissionChecker.IsGrantedAsync(_currentUser.UserId, Permissions.Archive.Delete);

        private ArchiveCommentDto Map(ArchiveComment comment, bool canModerate) => new()
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

        private static Error EntryNotFound() => new("ARCHIVE_ENTRY_NOT_FOUND", "Archive entry not found", ErrorType.NotFound);
    }
}
