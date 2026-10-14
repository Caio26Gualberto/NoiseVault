using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.PhotoTags.Models;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.PhotoTags.Interfaces
{
    public interface IPhotoTagRepository : IRepository<PhotoTag>
    {
        Task<PhotoTarget?> FindTargetAsync(int mediaId);
        Task<List<PhotoTagRow>> ListByMediaAsync(IReadOnlyCollection<int> mediaIds);
        Task<PhotoTagRow?> FindRowAsync(int tagId);
    }
}
