using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.Abstractions.Repository
{
    public interface IRepository<T> where T : EntityBase
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<T?> FirstOrDefaultAsync(QueryOptions<T> options);
        Task<List<T>> ListAsync(QueryOptions<T> options);
        Task<int> CountAsync(QueryOptions<T> options);
        Task AddAsync(T obj);
        Task AddRangeAsync(List<T> objs);
        Task UpdateAsync(T obj);
        Task DeleteAsync(T obj);
        Task SoftDelete(T obj);
    }
}
