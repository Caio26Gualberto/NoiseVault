using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Cities;
using NoiseVault.Application.Cities.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.Cities
{
    public class CityRepository : ICityRepository
    {
        private readonly AppDbContext _context;

        public CityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<City> Items, int TotalCount)> SearchAsync(string normalizedTerm, int skip, int take)
        {
            var query = _context.Cities.AsNoTracking().Where(c => c.SearchName.Contains(normalizedTerm));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.SearchName.StartsWith(normalizedTerm) ? 0 : 1)
                .ThenBy(c => CityRules.AbcCityIds.Contains(c.Id) ? 0 : 1)
                .ThenBy(c => c.SearchName)
                .ThenBy(c => c.Uf)
                .ThenBy(c => c.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<bool> ExistsAsync(int cityId) =>
            _context.Cities.AnyAsync(c => c.Id == cityId);

        public Task<List<City>> ListByIdsAsync(IReadOnlyCollection<int> cityIds) =>
            _context.Cities.AsNoTracking().Where(c => cityIds.Contains(c.Id)).ToListAsync();

        public Task<City?> FindAsync(int cityId) =>
            _context.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cityId);
    }
}
