using Microsoft.EntityFrameworkCore.Storage;
using Headbanger.Application.Abstractions.Repository;
using Headbanger.Application.Abstractions.Repository.UnitOfWork;
using Headbanger.Domain.Entities;
using Headbanger.Infra.Data.Context;

namespace Headbanger.Infra.Data.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Repository<T>() where T : EntityBase
        {
            var type = typeof(T);

            if (_repositories.ContainsKey(type))
                return (IRepository<T>)_repositories[type]!;

            var repository = new Repository<T>(_context);
            _repositories[type] = repository;

            return repository;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
                _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
