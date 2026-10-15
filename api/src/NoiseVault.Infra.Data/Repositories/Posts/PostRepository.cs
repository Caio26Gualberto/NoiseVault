using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Posts.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.Posts
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(List<PostReadModel> Items, int TotalCount)> PageVisibleAsync(PostScope scope, int viewerUserId, int skip, int take)
        {
            var query = Visible(viewerUserId);

            if (scope.AuthorUserId != null)
                query = query.Where(p => p.AuthorUserId == scope.AuthorUserId);

            if (scope.BandId != null)
                query = query.Where(p => p.BandId == scope.BandId);

            var totalCount = await query.CountAsync();

            var items = await Project(
                    query
                        .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                        .ThenByDescending(p => p.Id)
                        .Skip(skip)
                        .Take(take),
                    viewerUserId)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<PostReadModel?> FindVisibleAsync(int postId, int viewerUserId) =>
            Project(Visible(viewerUserId).Where(p => p.Id == postId), viewerUserId).FirstOrDefaultAsync();

        public async Task<bool> AddLikeAsync(int postId, int userId)
        {
            var likes = _context.Set<PostLike>();

            if (await likes.AnyAsync(l => l.PostId == postId && l.UserId == userId))
                return false;

            var like = new PostLike { PostId = postId, UserId = userId };
            likes.Add(like);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.Entry(like).State = EntityState.Detached;

                if (await likes.AsNoTracking().AnyAsync(l => l.PostId == postId && l.UserId == userId))
                    return false;

                throw;
            }
        }

        public Task RemoveLikeAsync(int postId, int userId) =>
            _context.Set<PostLike>()
                .Where(l => l.PostId == postId && l.UserId == userId)
                .ExecuteDeleteAsync();

        public async Task<bool> TryPublishAsync(int postId, DateTime now)
        {
            var affected = await _dbSet
                .Where(p => p.Id == postId
                    && p.PublishedAt == null
                    && !p.IsDeleted
                    && p.Media.All(m => m.MediaUpload!.Status == EMediaStatus.Ready))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.PublishedAt, now)
                    .SetProperty(p => p.UpdatedAt, now));

            return affected == 1;
        }

        public Task<List<int>> PendingPostIdsWithMediaAsync(int mediaUploadId) =>
            _context.Set<PostMedia>()
                .Where(pm => pm.MediaUploadId == mediaUploadId && pm.Post!.PublishedAt == null && !pm.Post.IsDeleted)
                .Select(pm => pm.PostId)
                .ToListAsync();

        public Task<int> CountPublishedByAuthorAsync(int userId) =>
            _dbSet.CountAsync(p => p.AuthorUserId == userId && p.PublishedAt != null && !p.IsDeleted);

        public Task<int> CountPublishedByBandAsync(int bandId) =>
            _dbSet.CountAsync(p => p.BandId == bandId && p.PublishedAt != null && !p.IsDeleted && p.Author!.IsActive);

        private IQueryable<Post> Visible(int viewerUserId) =>
            _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted
                    && p.Author!.IsActive
                    && (p.PublishedAt != null || p.AuthorUserId == viewerUserId));

        private static IQueryable<PostReadModel> Project(IQueryable<Post> query, int viewerUserId) =>
            query.Select(p => new PostReadModel
            {
                Id = p.Id,
                AuthorUserId = p.AuthorUserId,
                AuthorName = p.Author!.Name,
                AuthorNickname = p.Author.Nickname,
                AuthorAvatarKey = p.Author.AvatarKey,
                BandId = p.BandId,
                EventId = p.EventId,
                ArchiveEntryId = p.ArchiveEntryId,
                BandOpeningId = p.BandOpeningId,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                PublishedAt = p.PublishedAt,
                EditedAt = p.EditedAt,
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count(c => !c.IsDeleted && c.Author!.IsActive),
                LikedByMe = p.Likes.Any(l => l.UserId == viewerUserId),
                HasFailedMedia = p.Media.Any(m => m.MediaUpload!.Status == EMediaStatus.Failed),
                ViewerOwnsBand = p.BandId != null
                    && p.Band!.Members.Any(m => m.UserId == viewerUserId && m.Role == EBandMemberRole.Owner),
            });
    }
}
