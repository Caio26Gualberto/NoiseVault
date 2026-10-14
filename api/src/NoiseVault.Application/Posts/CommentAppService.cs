using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Mentions;
using NoiseVault.Application.Mentions.Dtos;
using NoiseVault.Application.Mentions.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Posts
{
    public class CommentAppService : ICommentAppService
    {
        private readonly IRepository<Comment> _commentRepository;
        private readonly IPostAppService _postAppService;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly INotificationAppService _notificationAppService;
        private readonly IMentionAppService _mentionAppService;
        private readonly IApplicationLogger _logger;

        public CommentAppService(
            IRepository<Comment> commentRepository,
            IPostAppService postAppService,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            INotificationAppService notificationAppService,
            IMentionAppService mentionAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _mentionAppService = mentionAppService;
            _commentRepository = commentRepository;
            _postAppService = postAppService;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Posts);
        }

        public async Task<Result<PagedResultDto<CommentDto>>> List(int postId, PagedRequestDto query)
        {
            query.Normalize();

            var post = await _postAppService.FindVisible(postId);

            if (post == null)
                return Result<PagedResultDto<CommentDto>>.Fail(PostNotFound());

            var options = new QueryOptions<Comment>
            {
                Predicate = c => c.PostId == postId && c.Author!.IsActive,
                Includes = [c => c.Author!],
                OrderBy = q => q.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _commentRepository.CountAsync(options);
            var comments = await _commentRepository.ListAsync(options);
            var mentions = await _mentionAppService.Resolve(comments.Select(c => c.Text));

            return Result<PagedResultDto<CommentDto>>.Ok(
                PagedResultDto<CommentDto>.Create(
                    comments.Select(c => Map(c, post.AuthorUserId, mentions)).ToList(),
                    query.Page,
                    query.PageSize,
                    totalCount),
                "COMMENTS_FOUND",
                "Comments found");
        }

        public async Task<Result<CommentDto>> Add(int postId, CommentInputDto input)
        {
            _logger.SetOperation("AddComment");

            var text = PostRules.NormalizeText(input.Text);

            if (text.Length is 0 or > PostRules.CommentMaxLength)
                return Result<CommentDto>.Fail(new Error("INVALID_COMMENT_TEXT", $"Comment must have between 1 and {PostRules.CommentMaxLength} characters", ErrorType.Validation));

            if (MentionRules.TooMany(text))
                return Result<CommentDto>.Fail(new Error("TOO_MANY_MENTIONS", $"A text can mention at most {MentionRules.MaxPerText} people or bands", ErrorType.Validation));

            var post = await _postAppService.FindVisible(postId);

            if (post == null)
                return Result<CommentDto>.Fail(PostNotFound());

            if (post.PublishedAt == null)
                return Result<CommentDto>.Fail(new Error("POST_NOT_PUBLISHED", "This post is still being processed", ErrorType.Conflict));

            var comment = new Comment
            {
                PostId = postId,
                AuthorUserId = _currentUser.UserId,
                Text = text,
            };

            await _commentRepository.AddAsync(comment);

            await _notificationAppService.Notify(post.AuthorUserId, new NotificationInput(ENotificationKind.PostCommented, comment.AuthorUserId, PostId: postId));

            await _mentionAppService.Notify(text, null, new MentionTarget(InComment: true, PostId: postId), comment.AuthorUserId, [post.AuthorUserId]);

            var saved = await _commentRepository.FirstOrDefaultAsync(new QueryOptions<Comment>
            {
                Predicate = c => c.Id == comment.Id,
                Includes = [c => c.Author!],
                AsNoTracking = true,
            });

            return Result<CommentDto>.Ok(Map(saved!, post.AuthorUserId, await _mentionAppService.Resolve([saved!.Text])), "COMMENT_CREATED", "Comment added");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteComment");

            var me = _currentUser.UserId;

            var comment = await _commentRepository.FirstOrDefaultAsync(new QueryOptions<Comment>
            {
                Predicate = c => c.Id == id && !c.Post!.IsDeleted,
                Includes = [c => c.Post!],
            });

            if (comment == null)
                return Result.Fail(new Error("COMMENT_NOT_FOUND", "Comment not found", ErrorType.NotFound));

            if (comment.AuthorUserId != me && comment.Post!.AuthorUserId != me)
                return Result.Fail(new Error("COMMENT_FORBIDDEN", "Only the comment author or the post author can delete it", ErrorType.Forbidden));

            await _commentRepository.SoftDelete(comment);

            _logger.Info("COMMENT_DELETED", "Comment deleted", userId: me, extra: new { CommentId = id, comment.PostId, ByPostAuthor = comment.AuthorUserId != me });

            return Result.Ok("COMMENT_DELETED", "Comment deleted");
        }

        private CommentDto Map(Comment comment, int postAuthorUserId, IReadOnlyDictionary<string, MentionDto> mentions)
        {
            var me = _currentUser.UserId;

            return new CommentDto
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
            Mentions = _mentionAppService.Pick(comment.Text, mentions),
                CreatedAt = comment.CreatedAt,
                CanDelete = comment.AuthorUserId == me || postAuthorUserId == me,
            };
        }

        private static Error PostNotFound() => new("POST_NOT_FOUND", "Post not found", ErrorType.NotFound);
    }
}
