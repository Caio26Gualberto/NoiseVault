using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.Cities.Interfaces
{
    public interface ICityRepository
    {
        Task<(List<City> Items, int TotalCount)> SearchAsync(string normalizedTerm, int skip, int take);
        Task<bool> ExistsAsync(int cityId);
        Task<City?> FindAsync(int cityId);
    }
}
