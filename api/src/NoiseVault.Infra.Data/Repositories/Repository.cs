using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Domain.Entities;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : EntityBase
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDelete(T obj)
        {
            obj.IsDeleted = true;
            _dbSet.Update(obj);
            await _context.SaveChangesAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<T?> FirstOrDefaultAsync(QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;

            if (options.AsNoTracking)
                query = query.AsNoTracking();

            if (!options.IncludeDeleted)
                query = query.Where(x => !x.IsDeleted);

            if (options.Predicate is not null)
                query = query.Where(options.Predicate);

            foreach (var include in options.Includes)
                query = query.Include(include);

            if (options.OrderBy is not null)
                query = options.OrderBy(query);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<T>> ListAsync(QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;

            if (options.AsNoTracking)
                query = query.AsNoTracking();

            if (!options.IncludeDeleted)
                query = query.Where(x => !x.IsDeleted);

            if (options.Predicate is not null)
                query = query.Where(options.Predicate);

            foreach (var include in options.Includes)
                query = query.Include(include);

            if (options.OrderBy is not null)
                query = options.OrderBy(query);

            if (options.Skip.HasValue)
                query = query.Skip(options.Skip.Value);

            if (options.Take.HasValue)
                query = query.Take(options.Take.Value);

            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;

            if (!options.IncludeDeleted)
                query = query.Where(x => !x.IsDeleted);

            if (options.Predicate is not null)
                query = query.Where(options.Predicate);

            return await query.CountAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}
