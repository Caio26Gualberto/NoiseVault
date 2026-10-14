using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.PhotoTags.Dtos;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Mentions;
using NoiseVault.Application.Mentions.Interfaces;
using NoiseVault.Application.PhotoTags.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Posts.Models;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.PublicProfiles.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Posts;

namespace NoiseVault.Application.Posts
{
    public class PostAppService : IPostAppService
    {
        private readonly IPostRepository _postRepository;
        private readonly IRepository<PostMedia> _postMediaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IBandAppService _bandAppService;
        private readonly IPublicProfileAppService _publicProfileAppService;
        private readonly IMediaUploadAppService _mediaAppService;
        private readonly IFileStorage _fileStorage;
        private readonly IEventRepository _eventRepository;
        private readonly IArchiveRepository _archiveRepository;
        private readonly IRepository<MediaUpload> _mediaRepository;
        private readonly INotificationAppService _notificationAppService;
        private readonly IPhotoTagAppService _photoTagAppService;
        private readonly IMentionAppService _mentionAppService;
        private readonly IApplicationLogger _logger;

        public PostAppService(
            IPostRepository postRepository,
            IRepository<PostMedia> postMediaRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IBandAppService bandAppService,
            IPublicProfileAppService publicProfileAppService,
            IMediaUploadAppService mediaAppService,
            IFileStorage fileStorage,
            IEventRepository eventRepository,
            IArchiveRepository archiveRepository,
            IRepository<MediaUpload> mediaRepository,
            INotificationAppService notificationAppService,
            IPhotoTagAppService photoTagAppService,
            IMentionAppService mentionAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _photoTagAppService = photoTagAppService;
            _mentionAppService = mentionAppService;
            _archiveRepository = archiveRepository;
            _mediaRepository = mediaRepository;
            _eventRepository = eventRepository;
            _postRepository = postRepository;
            _postMediaRepository = postMediaRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _bandAppService = bandAppService;
            _publicProfileAppService = publicProfileAppService;
            _mediaAppService = mediaAppService;
            _fileStorage = fileStorage;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Posts);
        }

        public Task<Result<PagedResultDto<PostDto>>> Feed(PagedRequestDto query) =>
            PageAsync(PostScope.Everyone, query);

        public async Task<Result<PagedResultDto<PostDto>>> ByUser(string nickname, PagedRequestDto query)
        {
            var userId = await _publicProfileAppService.FindUserId(nickname);

            if (userId == null)
                return Result<PagedResultDto<PostDto>>.Fail(new Error("PROFILE_NOT_FOUND", "Profile not found", ErrorType.NotFound));

            return await PageAsync(new PostScope(AuthorUserId: userId), query);
        }

        public async Task<Result<PagedResultDto<PostDto>>> ByBand(string slug, PagedRequestDto query)
        {
            var (bandId, _) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Result<PagedResultDto<PostDto>>.Fail(new Error("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound));

            return await PageAsync(new PostScope(BandId: bandId), query);
        }

        public async Task<Result<PostDto>> Get(int id)
        {
            var post = await FindVisible(id);

            return post == null
                ? Result<PostDto>.Fail(PostNotFound())
                : Result<PostDto>.Ok(await MapOneAsync(post), "POST_FOUND", "Post found");
        }

        public async Task<Result<PostDto>> Create(CreatePostInputDto input)
        {
            _logger.SetOperation("CreatePost");

            var content = PostRules.NormalizeText(input.Content);

            if (content.Length > PostRules.ContentMaxLength)
                return Fail(Invalid("INVALID_POST_CONTENT", $"Post text must have at most {PostRules.ContentMaxLength} characters"));

            if (MentionRules.TooMany(content))
                return Fail(TooManyMentions());

            var mediaIds = (input.MediaIds ?? []).Distinct().ToList();

            if (content.Length == 0 && mediaIds.Count == 0)
                return Fail(Invalid("POST_EMPTY", "Write something or attach a photo or video"));

            if (mediaIds.Count > PostRules.MaxMediaPerPost)
                return Fail(TooManyPhotos());

            int? bandId = null;

            if (!string.IsNullOrWhiteSpace(input.BandSlug))
            {
                var (foundBandId, role) = await _bandAppService.FindAccess(input.BandSlug);

                if (foundBandId == null)
                    return Fail(new Error("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound));

                if (role == null)
                    return Fail(new Error("NOT_BAND_MEMBER", "Only band members can post as the band", ErrorType.Forbidden));

                bandId = foundBandId;
            }

            var me = _currentUser.UserId;
            var media = await _mediaAppService.ListAttachable(mediaIds, me, EMediaProfile.Post);

            if (media.Count != mediaIds.Count)
                return Fail(new Error("MEDIA_NOT_ATTACHABLE", "Some attachments are not available anymore; upload them again", ErrorType.Conflict));

            if (media.Count(m => m.Kind == EMediaKind.Photo) > PostRules.MaxPhotosPerPost)
                return Fail(TooManyPhotos());

            if (media.Count(m => m.Kind == EMediaKind.Video) > PostRules.MaxVideosPerPost)
                return Fail(Invalid("TOO_MANY_VIDEOS", $"A post can have at most {PostRules.MaxVideosPerPost} video"));

            var allReady = media.All(m => m.Status == EMediaStatus.Ready);

            var post = new Post
            {
                AuthorUserId = me,
                BandId = bandId,
                Content = content,
                PublishedAt = allReady ? DateTime.UtcNow : null,
            };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _postRepository.AddAsync(post);

                if (mediaIds.Count > 0)
                    await _postMediaRepository.AddRangeAsync(mediaIds
                        .Select((mediaId, index) => new PostMedia { PostId = post.Id, MediaUploadId = mediaId, Order = index })
                        .ToList());

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var published = allReady || await _postRepository.TryPublishAsync(post.Id, DateTime.UtcNow);

            _logger.Info("POST_CREATED", "Post created", userId: me, extra: new { PostId = post.Id, BandId = bandId, Media = mediaIds.Count, Published = published });

            await _mentionAppService.Notify(content, null, new MentionTarget(InComment: false, PostId: post.Id), me, []);

            var created = await FindVisible(post.Id);

            return Result<PostDto>.Ok(await MapOneAsync(created!), "POST_CREATED", published ? "Post published" : "Post created; it is published when the video is ready");
        }

        public async Task<Result<PostDto>> Update(int id, UpdatePostInputDto input)
        {
            _logger.SetOperation("UpdatePost");

            var content = PostRules.NormalizeText(input.Content);

            if (content.Length > PostRules.ContentMaxLength)
                return Fail(Invalid("INVALID_POST_CONTENT", $"Post text must have at most {PostRules.ContentMaxLength} characters"));

            if (MentionRules.TooMany(content))
                return Fail(TooManyMentions());

            var me = _currentUser.UserId;

            var post = await _postRepository.FirstOrDefaultAsync(new QueryOptions<Post>
            {
                Predicate = p => p.Id == id && p.AuthorUserId == me,
                Includes = [p => p.Media],
            });

            if (post == null)
                return Fail(await DeniedAsync(id));

            if (content.Length == 0 && post.Media.Count == 0 && post.EventId == null && post.ArchiveEntryId == null)
                return Fail(Invalid("POST_EMPTY", "Write something or attach a photo or video"));

            if (content != post.Content)
            {
                var previous = post.Content;
                post.Content = content;
                post.EditedAt = DateTime.UtcNow;
                await _postRepository.UpdateAsync(post);

                _logger.Info("POST_UPDATED", "Post text edited", userId: me, extra: new { PostId = post.Id });

                await _mentionAppService.Notify(content, previous, new MentionTarget(InComment: false, PostId: post.Id), me, []);
            }

            return Result<PostDto>.Ok(await MapOneAsync((await FindVisible(id))!), "POST_UPDATED", "Post updated");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeletePost");

            var me = _currentUser.UserId;

            var post = await _postRepository.FirstOrDefaultAsync(new QueryOptions<Post>
            {
                Predicate = p => p.Id == id
                    && (p.AuthorUserId == me
                        || (p.BandId != null && p.Band!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner))),
            });

            if (post == null)
                return Result.Fail(await DeniedAsync(id));

            await _postRepository.SoftDelete(post);

            var media = await _postMediaRepository.ListAsync(new QueryOptions<PostMedia>
            {
                Predicate = pm => pm.PostId == id,
                Includes = [pm => pm.MediaUpload!],
                AsNoTracking = true,
            });

            await _mediaAppService.DeleteStoredFiles(media.Select(pm => pm.MediaUpload!).ToList());

            _logger.Info("POST_DELETED", "Post deleted", userId: me, extra: new { PostId = id, post.BandId, ByBandOwner = post.AuthorUserId != me });

            return Result.Ok("POST_DELETED", "Post deleted");
        }

        public Task<Result<PostDto>> Like(int id) => ReactAsync(id, like: true);

        public Task<Result<PostDto>> Unlike(int id) => ReactAsync(id, like: false);

        public Task<PostReadModel?> FindVisible(int postId) =>
            _postRepository.FindVisibleAsync(postId, _currentUser.UserId);

        public async Task ProcessVideo(int mediaUploadId, bool finalAttempt)
        {
            var status = await _mediaAppService.ProcessVideo(mediaUploadId, finalAttempt);

            if (status != EMediaStatus.Ready)
                return;

            foreach (var postId in await _postRepository.PendingPostIdsWithMediaAsync(mediaUploadId))
            {
                if (await _postRepository.TryPublishAsync(postId, DateTime.UtcNow))
                    _logger.Info("POST_PUBLISHED", "Post published after its video was processed", extra: new { PostId = postId, MediaId = mediaUploadId });
            }
        }

        public Task CreateForEvent(int eventId, int authorUserId, int? bandId) =>
            _postRepository.AddAsync(new Post
            {
                AuthorUserId = authorUserId,
                BandId = bandId,
                EventId = eventId,
                Content = string.Empty,
                PublishedAt = DateTime.UtcNow,
            });

        public Task CreateForArchive(int archiveEntryId, int authorUserId) =>
            _postRepository.AddAsync(new Post
            {
                AuthorUserId = authorUserId,
                ArchiveEntryId = archiveEntryId,
                Content = string.Empty,
                PublishedAt = DateTime.UtcNow,
            });

        public async Task DeleteForArchive(int archiveEntryId)
        {
            var post = await _postRepository.FirstOrDefaultAsync(new QueryOptions<Post> { Predicate = p => p.ArchiveEntryId == archiveEntryId });

            if (post != null)
                await _postRepository.SoftDelete(post);
        }

        public async Task DeleteForEvent(int eventId)
        {
            var post = await _postRepository.FirstOrDefaultAsync(new QueryOptions<Post> { Predicate = p => p.EventId == eventId });

            if (post != null)
                await _postRepository.SoftDelete(post);
        }

        private async Task<Result<PostDto>> ReactAsync(int id, bool like)
        {
            var me = _currentUser.UserId;
            var post = await FindVisible(id);

            if (post == null)
                return Fail(PostNotFound());

            if (post.PublishedAt == null)
                return Fail(new Error("POST_NOT_PUBLISHED", "This post is still being processed", ErrorType.Conflict));

            if (like)
            {
                if (await _postRepository.AddLikeAsync(id, me))
                    await _notificationAppService.Notify(post.AuthorUserId, new NotificationInput(ENotificationKind.PostLiked, me, PostId: id));
            }
            else
                await _postRepository.RemoveLikeAsync(id, me);

            return Result<PostDto>.Ok(
                await MapOneAsync((await FindVisible(id))!),
                like ? "POST_LIKED" : "POST_UNLIKED",
                like ? "Post liked" : "Like removed");
        }

        private async Task<Result<PagedResultDto<PostDto>>> PageAsync(PostScope scope, PagedRequestDto query)
        {
            query.Normalize();

            var (rows, totalCount) = await _postRepository.PageVisibleAsync(scope, _currentUser.UserId, query.Skip, query.PageSize);

            return Result<PagedResultDto<PostDto>>.Ok(
                PagedResultDto<PostDto>.Create(await MapAsync(rows), query.Page, query.PageSize, totalCount),
                "POSTS_FOUND",
                "Posts found");
        }

        private async Task<Error> DeniedAsync(int id) =>
            await FindVisible(id) == null
                ? PostNotFound()
                : new Error("POST_FORBIDDEN", "Only the author can do this", ErrorType.Forbidden);

        private async Task<PostDto> MapOneAsync(PostReadModel post) => (await MapAsync([post]))[0];

        private async Task<List<PostDto>> MapAsync(List<PostReadModel> rows)
        {
            if (rows.Count == 0)
                return [];

            var postIds = rows.Select(r => r.Id).ToList();
            var bandIds = rows.Where(r => r.BandId != null).Select(r => r.BandId!.Value).Distinct().ToList();

            var bands = await _bandAppService.SummariesByIds(bandIds);

            var archives = await ArchiveSummariesAsync(rows);

            var eventIds = rows.Where(r => r.EventId != null).Select(r => r.EventId!.Value).Distinct().ToList();
            var now = DateTime.UtcNow;
            var events = eventIds.Count == 0
                ? []
                : (await _eventRepository.ListByIdsAsync(eventIds, _currentUser.UserId)).ToDictionary(e => e.Id, e => new EventSummaryDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    BannerUrl = _fileStorage.PublicUrl(e.BannerKey),
                    BannerPosition = e.BannerPosition,
                    StartsAt = e.StartsAt,
                    Venue = e.Venue,
                    City = e.City,
                    CityName = e.CityName,
                    Status = e.Status,
                    HasEnded = e.EndsAtOrDefault < now,
                });

            var media = (await _postMediaRepository.ListAsync(new QueryOptions<PostMedia>
            {
                Predicate = pm => postIds.Contains(pm.PostId),
                Includes = [pm => pm.MediaUpload!],
                OrderBy = q => q.OrderBy(pm => pm.PostId).ThenBy(pm => pm.Order),
                AsNoTracking = true,
            })).ToLookup(pm => pm.PostId);

            var me = _currentUser.UserId;
            var mentions = await _mentionAppService.Resolve(rows.Select(r => r.Content));
            var tags = await _photoTagAppService.ForMedia(media.SelectMany(g => g).Select(pm => pm.MediaUploadId).ToList());

            return rows.Select(r => new PostDto
            {
                Id = r.Id,
                Author = new ProfileSummaryDto
                {
                    Id = r.AuthorUserId,
                    Name = r.AuthorName,
                    Nickname = r.AuthorNickname,
                    AvatarUrl = _fileStorage.PublicUrl(r.AuthorAvatarKey),
                },
                Band = r.BandId != null && bands.TryGetValue(r.BandId.Value, out var band) ? band : null,
                Event = r.EventId != null && events.TryGetValue(r.EventId.Value, out var linkedEvent) ? linkedEvent : null,
                Archive = r.ArchiveEntryId != null && archives.TryGetValue(r.ArchiveEntryId.Value, out var linkedArchive) ? linkedArchive : null,
                Content = r.Content,
                Mentions = _mentionAppService.Pick(r.Content, mentions),
                Media = media[r.Id].Select(pm => WithTags(_mediaAppService.Describe(pm.MediaUpload!), tags)).ToList(),
                LikeCount = r.LikeCount,
                CommentCount = r.CommentCount,
                LikedByMe = r.LikedByMe,
                Status = r.PublishedAt != null
                    ? EPostStatus.Published
                    : r.HasFailedMedia ? EPostStatus.Failed : EPostStatus.Processing,
                CreatedAt = r.PublishedAt ?? r.CreatedAt,
                EditedAt = r.EditedAt,
                CanEdit = r.AuthorUserId == me,
                CanDelete = r.AuthorUserId == me || r.ViewerOwnsBand,
            }).ToList();
        }

        private async Task<Dictionary<int, ArchiveSummaryDto>> ArchiveSummariesAsync(List<PostReadModel> rows)
        {
            var ids = rows.Where(r => r.ArchiveEntryId != null).Select(r => r.ArchiveEntryId!.Value).Distinct().ToList();

            if (ids.Count == 0)
                return [];

            var entries = await _archiveRepository.ListByIdsAsync(ids, _currentUser.UserId);
            var coverIds = entries.Where(e => e.CoverMediaUploadId != null).Select(e => e.CoverMediaUploadId!.Value).ToList();
            var covers = (await _mediaRepository.ListAsync(new QueryOptions<MediaUpload> { Predicate = m => coverIds.Contains(m.Id), AsNoTracking = true }))
                .ToDictionary(m => m.Id);

            return entries.ToDictionary(e => e.Id, e => new ArchiveSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Year = e.Year,
                Month = e.Month,
                Day = e.Day,
                Cover = e.CoverMediaUploadId != null && covers.TryGetValue(e.CoverMediaUploadId.Value, out var cover) ? _mediaAppService.Describe(cover) : null,
                PhotoCount = e.PhotoCount,
                VideoCount = e.VideoCount,
            });
        }

        private static Result<PostDto> Fail(Error error) => Result<PostDto>.Fail(error);

        private static Error Invalid(string code, string message) => new(code, message, ErrorType.Validation);

        private static Error TooManyPhotos() =>
            Invalid("TOO_MANY_PHOTOS", $"A post can have at most {PostRules.MaxPhotosPerPost} photos and {PostRules.MaxVideosPerPost} video");

        private static MediaDto WithTags(MediaDto media, Dictionary<int, List<PhotoTagDto>> tags)
        {
            media.Tags = tags.GetValueOrDefault(media.Id) ?? [];
            return media;
        }

        private static Error TooManyMentions() =>
            Invalid("TOO_MANY_MENTIONS", $"A text can mention at most {MentionRules.MaxPerText} people or bands");

        private static Error PostNotFound() => new("POST_NOT_FOUND", "Post not found", ErrorType.NotFound);
    }
}
