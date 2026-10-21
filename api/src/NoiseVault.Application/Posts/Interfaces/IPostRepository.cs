using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Posts.Models;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.Posts.Interfaces
{
    public interface IPostRepository : IRepository<Post>
    {
        Task<(List<PostReadModel> Items, int TotalCount)> PageVisibleAsync(PostScope scope, int viewerUserId, int skip, int take);
        Task<PostReadModel?> FindVisibleAsync(int postId, int viewerUserId);
        Task<bool> AddLikeAsync(int postId, int userId);
        Task RemoveLikeAsync(int postId, int userId);
        Task<(List<User> Users, int TotalCount)> PageLikersAsync(int postId, int skip, int take);
        Task<Dictionary<int, List<User>>> LikerPreviewsAsync(List<int> postIds, int take);
        Task<bool> TryPublishAsync(int postId, DateTime now);
        Task<List<int>> PendingPostIdsWithMediaAsync(int mediaUploadId);
        Task<int> CountPublishedByAuthorAsync(int userId);
        Task<int> CountPublishedByBandAsync(int bandId);
    }
}
