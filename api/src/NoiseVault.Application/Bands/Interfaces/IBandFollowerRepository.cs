namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandFollowerRepository
    {
        Task<bool> AddAsync(int bandId, int userId);
        Task<bool> RemoveAsync(int bandId, int userId);
        Task<bool> IsFollowingAsync(int bandId, int userId);
        Task<int> CountAsync(int bandId);
        Task<List<int>> FollowerUserIdsAsync(int bandId);
        Task<(List<int> BandIds, int TotalCount)> PageFollowedBandIdsAsync(int userId, int skip, int take);
    }
}
