using Headbanger.Domain.Entities;

namespace Headbanger.Application.Abstractions.Repository.UnitOfWork
{
    public interface IUnitOfWork
    {
        IRepository<T> Repository<T>() where T : EntityBase;
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
