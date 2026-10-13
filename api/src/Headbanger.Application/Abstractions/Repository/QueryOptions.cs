using Headbanger.Domain.Entities;
using System.Linq.Expressions;

namespace Headbanger.Application.Abstractions.Repository
{
    public sealed class QueryOptions<T> where T : EntityBase
    {
        public Expression<Func<T, bool>>? Predicate { get; init; }
        public Expression<Func<T, object>>[] Includes { get; init; } = [];
        public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; init; }
        public bool AsNoTracking { get; init; } = false;
        public bool IncludeDeleted { get; init; } = false;
        public int? Skip { get; init; }
        public int? Take { get; init; }
    }
}
