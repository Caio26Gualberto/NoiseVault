using Microsoft.EntityFrameworkCore;
using Headbanger.Application.AuditLogs.Dtos;
using Headbanger.Application.AuditLogs.Interfaces;
using Headbanger.Domain.Entities;
using Headbanger.Infra.Data.Context;

namespace Headbanger.Infra.Data.Repositories.AuditLogs
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _context;

        public AuditLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<AuditLogListItemDto> Items, int TotalCount)> ListAsync(
            AuditLogQueryDto query,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken ct = default)
        {
            var filtered = Filter(query, fromUtc, toUtc);

            var totalCount = await filtered.CountAsync(ct);

            var items = await filtered
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id)
                .Skip(query.Skip)
                .Take(query.PageSize)
                .Select(a => new AuditLogListItemDto
                {
                    Id = a.Id,
                    OccurredAt = a.OccurredAt,
                    ActorUserId = a.ActorUserId,
                    ActorName = a.ActorName,
                    ActorEmail = a.ActorEmail,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    EntityDescription = a.EntityDescription,
                    IpAddress = a.IpAddress,
                    ChangesJson = a.ChangesJson,
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<AuditLogFiltersDto> GetFiltersAsync(CancellationToken ct = default)
        {
            var entityTypes = await _context.AuditLogs
                .AsNoTracking()
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(type => type)
                .ToListAsync(ct);

            var actors = await _context.AuditLogs
                .AsNoTracking()
                .GroupBy(a => new { a.ActorUserId, a.ActorName, a.ActorEmail })
                .Select(g => new AuditLogActorDto
                {
                    AccountId = g.Key.ActorUserId,
                    Name = g.Key.ActorName,
                    Email = g.Key.ActorEmail,
                })
                .OrderBy(actor => actor.Name)
                .ToListAsync(ct);

            return new AuditLogFiltersDto
            {
                EntityTypes = entityTypes,
                Actors = actors,
            };
        }

        private IQueryable<AuditLog> Filter(AuditLogQueryDto query, DateTime? fromUtc, DateTime? toUtc) =>
            _context.AuditLogs
                .AsNoTracking()
                .Where(a => (fromUtc == null || a.OccurredAt >= fromUtc)
                    && (toUtc == null || a.OccurredAt < toUtc)
                    && (query.EntityType == null || a.EntityType == query.EntityType)
                    && (query.Action == null || a.Action == query.Action)
                    && (query.ActorUserId == null || a.ActorUserId == query.ActorUserId)
                    && (query.EntityId == null || a.EntityId == query.EntityId));
    }
}
