using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.BandFollowers
{
    public class BandFollowerRepository : IBandFollowerRepository
    {
        private readonly AppDbContext _context;

        public BandFollowerRepository(AppDbContext context)
        {
            _context = context;
        }

        private DbSet<BandFollower> Followers => _context.Set<BandFollower>();

        public async Task<bool> AddAsync(int bandId, int userId)
        {
            if (await Followers.AnyAsync(f => f.BandId == bandId && f.UserId == userId))
                return false;

            var follower = new BandFollower { BandId = bandId, UserId = userId };
            Followers.Add(follower);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.Entry(follower).State = EntityState.Detached;

                if (await Followers.AsNoTracking().AnyAsync(f => f.BandId == bandId && f.UserId == userId))
                    return false;

                throw;
            }
        }

        public async Task<bool> RemoveAsync(int bandId, int userId) =>
            await Followers
                .Where(f => f.BandId == bandId && f.UserId == userId)
                .ExecuteDeleteAsync() == 1;

        public Task<bool> IsFollowingAsync(int bandId, int userId) =>
            Followers.AsNoTracking().AnyAsync(f => f.BandId == bandId && f.UserId == userId);

        public Task<int> CountAsync(int bandId) =>
            Followers.CountAsync(f => f.BandId == bandId && f.User!.IsActive);

        public Task<List<int>> FollowerUserIdsAsync(int bandId) =>
            Followers
                .Where(f => f.BandId == bandId && f.User!.IsActive)
                .Select(f => f.UserId)
                .ToListAsync();

        public async Task<(List<int> BandIds, int TotalCount)> PageFollowedBandIdsAsync(int userId, int skip, int take)
        {
            var query = Followers.AsNoTracking().Where(f => f.UserId == userId && !f.Band!.IsDeleted);

            var totalCount = await query.CountAsync();

            var bandIds = await query
                .OrderByDescending(f => f.CreatedAt)
                .ThenByDescending(f => f.BandId)
                .Skip(skip)
                .Take(take)
                .Select(f => f.BandId)
                .ToListAsync();

            return (bandIds, totalCount);
        }
    }
}
