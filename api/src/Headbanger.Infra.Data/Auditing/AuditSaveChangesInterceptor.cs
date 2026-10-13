using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Headbanger.Application.Abstractions.Authentication;
using Headbanger.Domain.Auditing;
using Headbanger.Domain.Constants;
using Headbanger.Domain.Entities;
using Headbanger.Domain.Enums.Audit;

namespace Headbanger.Infra.Data.Auditing
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUser _currentUser;
        private readonly List<PendingAudit> _pending = [];
        private bool _writingAudit;

        public AuditSaveChangesInterceptor(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Capture(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Capture(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Persist(eventData.Context);
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            await PersistAsync(eventData.Context, cancellationToken);
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending.Clear();

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _pending.Clear();
            return Task.CompletedTask;
        }

        private void Capture(DbContext? context)
        {
            if (_writingAudit || context is null)
                return;

            _pending.Clear();

            if (_currentUser.UserIdOrNull is null || _currentUser.Role is null || _currentUser.Role == Roles.User)
                return;

            foreach (var entry in context.ChangeTracker.Entries<EntityBase>())
            {
                var entityType = entry.Metadata.ClrType.Name;

                if (!AuditPolicy.IsAudited(entityType))
                    continue;

                var action = ResolveAction(entry);

                if (action is null)
                    continue;

                var changes = BuildChanges(entry, entityType, action.Value);

                if (action == EAuditAction.Updated && changes.Count == 0)
                    continue;

                _pending.Add(new PendingAudit
                {
                    Entity = entry.Entity,
                    Action = action.Value,
                    EntityType = entityType,
                    Description = ResolveDescription(entry),
                    Changes = changes,
                });
            }
        }

        private void Persist(DbContext? context)
        {
            if (!TryBuildLogs(context, out var logs))
                return;

            _writingAudit = true;
            try
            {
                context!.Set<AuditLog>().AddRange(logs);
                context.SaveChanges();
            }
            finally
            {
                _writingAudit = false;
            }
        }

        private async Task PersistAsync(DbContext? context, CancellationToken cancellationToken)
        {
            if (!TryBuildLogs(context, out var logs))
                return;

            _writingAudit = true;
            try
            {
                context!.Set<AuditLog>().AddRange(logs);
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _writingAudit = false;
            }
        }

        private bool TryBuildLogs(DbContext? context, out List<AuditLog> logs)
        {
            logs = [];

            if (_writingAudit || context is null || _pending.Count == 0)
                return false;

            var pending = _pending.ToList();
            _pending.Clear();

            var occurredAt = DateTime.UtcNow;
            var actorId = _currentUser.UserIdOrNull ?? 0;
            var actorEmail = SafeRead(() => _currentUser.Email);
            var actorName = SafeRead(() => _currentUser.Name);
            var ipAddress = SafeRead(() => _currentUser.IpAddress);

            logs = pending.Select(item => new AuditLog
            {
                OccurredAt = occurredAt,
                ActorUserId = actorId,
                ActorEmail = actorEmail ?? string.Empty,
                ActorName = actorName ?? string.Empty,
                Action = item.Action,
                EntityType = item.EntityType,
                EntityId = item.Entity.Id.ToString(CultureInfo.InvariantCulture),
                EntityDescription = item.Description,
                ChangesJson = item.Changes.Count == 0 ? null : JsonSerializer.Serialize(item.Changes),
                IpAddress = ipAddress,
            }).ToList();

            return logs.Count > 0;
        }

        private static EAuditAction? ResolveAction(EntityEntry<EntityBase> entry) => entry.State switch
        {
            EntityState.Added => EAuditAction.Created,
            EntityState.Deleted => EAuditAction.HardDeleted,
            EntityState.Modified => IsSoftDeleting(entry) ? EAuditAction.SoftDeleted : EAuditAction.Updated,
            _ => null,
        };

        private static bool IsSoftDeleting(EntityEntry<EntityBase> entry)
        {
            var property = entry.Property(nameof(EntityBase.IsDeleted));

            return property.CurrentValue is true && property.OriginalValue is not true;
        }

        private static List<AuditFieldChange> BuildChanges(
            EntityEntry<EntityBase> entry,
            string entityType,
            EAuditAction action)
        {
            var changes = new List<AuditFieldChange>();

            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;

                if (AuditPolicy.IsOmitted(name))
                    continue;

                if (action == EAuditAction.Updated || action == EAuditAction.SoftDeleted)
                {
                    if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                        continue;
                }

                if (AuditPolicy.IsMasked(entityType, name))
                {
                    changes.Add(new AuditFieldChange { Field = name, Masked = true });
                    continue;
                }

                changes.Add(new AuditFieldChange
                {
                    Field = name,
                    OldValue = action == EAuditAction.Created ? null : ToDisplay(property.OriginalValue),
                    NewValue = action == EAuditAction.HardDeleted ? null : ToDisplay(property.CurrentValue),
                });
            }

            return changes;
        }

        private static string? ResolveDescription(EntityEntry<EntityBase> entry)
        {
            string? description = null;
            var bestPriority = int.MaxValue;

            foreach (var property in entry.Properties)
            {
                if (!AuditPolicy.IsDescriptionProperty(property.Metadata.Name))
                    continue;

                var priority = AuditPolicy.DescriptionPriority(property.Metadata.Name);

                if (priority >= bestPriority)
                    continue;

                var value = ToDisplay(property.CurrentValue);

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                description = value;
                bestPriority = priority;
            }

            return description;
        }

        private static string? ToDisplay(object? value) => value switch
        {
            null => null,
            string text => text,
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };

        private static T? SafeRead<T>(Func<T?> read)
        {
            try
            {
                return read();
            }
            catch (UnauthorizedAccessException)
            {
                return default;
            }
        }

        private sealed class PendingAudit
        {
            public required EntityBase Entity { get; init; }
            public required EAuditAction Action { get; init; }
            public required string EntityType { get; init; }
            public string? Description { get; init; }
            public required List<AuditFieldChange> Changes { get; init; }
        }
    }
}
