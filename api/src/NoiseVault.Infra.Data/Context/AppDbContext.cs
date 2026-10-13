using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Domain.Entities;
using NoiseVault.Infra.Data.Identity;

namespace NoiseVault.Infra.Data.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public const string CaseInsensitiveCollation = "case_insensitive";

        private readonly ICurrentUser _currentUserContext;

        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUserContext) : base(options)
        {
            _currentUserContext = currentUserContext;
        }

        public new DbSet<User> Users => Set<User>();
        public DbSet<UserGenre> UserGenres => Set<UserGenre>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AuthorizationAuditLog> AuthorizationAuditLogs => Set<AuthorizationAuditLog>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var isPostgres = Database.IsNpgsql();
            var jsonType = DatabaseProvider.JsonColumnType(this);

            if (isPostgres)
                builder.HasCollation(
                    CaseInsensitiveCollation,
                    locale: "und-u-ks-level2",
                    provider: "icu",
                    deterministic: false);

            builder.Entity<ApplicationUser>().ToTable("UsersIdentity");
            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("Roles");
                b.Property(r => r.Description).HasMaxLength(256);
            });
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            builder.Entity<User>(b =>
            {
                b.ToTable("Users");
                b.HasKey(u => u.Id);

                b.Property(u => u.Name).HasMaxLength(256);
                b.Property(u => u.Nickname).HasMaxLength(30).IsRequired();
                b.HasIndex(u => u.Nickname).IsUnique();

                var email = b.Property(u => u.Email).HasMaxLength(256);
                if (isPostgres)
                    email.UseCollation(CaseInsensitiveCollation);

                b.HasIndex(u => u.Email).IsUnique();

                b.Property(u => u.Bio).HasMaxLength(500);
                b.Property(u => u.MainInstrument).HasMaxLength(60);
                b.Property(u => u.InstagramUrl).HasMaxLength(300);
                b.Property(u => u.YoutubeUrl).HasMaxLength(300);
                b.Property(u => u.SpotifyUrl).HasMaxLength(300);
                b.Property(u => u.AvatarKey).HasMaxLength(200);
                b.Property(u => u.BannerKey).HasMaxLength(200);
                b.Property(u => u.BannerPosition).HasDefaultValue(User.DefaultBannerPosition);

                b.HasMany(u => u.Genres)
                    .WithOne()
                    .HasForeignKey(g => g.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserGenre>(b =>
            {
                b.ToTable("UserGenres");
                b.HasKey(g => new { g.UserId, g.Genre });
            });

            builder.Entity<ApplicationUser>(b =>
            {
                b.HasOne(a => a.User)
                    .WithOne()
                    .HasForeignKey<ApplicationUser>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(a => a.UserName).IsUnique();

                if (isPostgres)
                    b.Property(a => a.Email).UseCollation(CaseInsensitiveCollation);
            });

            builder.Entity<RefreshToken>(b =>
            {
                b.ToTable("RefreshTokens");
                b.HasKey(r => r.Id);

                b.Property(r => r.TokenHash).HasMaxLength(64);
                b.HasIndex(r => r.TokenHash).IsUnique();

                b.HasOne(r => r.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(r => r.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AuthorizationAuditLog>(b =>
            {
                b.ToTable("AuthorizationAuditLogs");
                b.HasKey(a => a.Id);

                b.Property(a => a.Action).HasMaxLength(64);
                b.Property(a => a.TargetType).HasMaxLength(16);
                b.Property(a => a.TargetId).HasMaxLength(64);
                b.Property(a => a.TargetName).HasMaxLength(256);
                b.Property(a => a.ActorEmail).HasMaxLength(256);
                b.Property(a => a.IpAddress).HasMaxLength(64);
                b.Property(a => a.OldValueJson).HasColumnType(jsonType);
                b.Property(a => a.NewValueJson).HasColumnType(jsonType);

                b.HasIndex(a => a.CreatedAt);
                b.HasIndex(a => a.Action);
            });

            builder.Entity<BackgroundJobRun>(b =>
            {
                b.ToTable("BackgroundJobRuns");
                b.HasKey(j => j.Id);

                b.Property(j => j.RequestedByEmail).HasMaxLength(256);
                b.Property(j => j.DeduplicationKey).HasMaxLength(200);
                b.Property(j => j.ErrorMessage).HasMaxLength(2000);
                b.Property(j => j.ResultStorageKey).HasMaxLength(400);
                b.Property(j => j.HangfireJobId).HasMaxLength(64);
                b.Property(j => j.PayloadJson).HasColumnType(jsonType);
                b.Property(j => j.ResultJson).HasColumnType(jsonType);

                b.HasOne(j => j.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(j => j.RequestedByUserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(j => new { j.JobType, j.CreatedAt });
                b.HasIndex(j => new { j.Status, j.CreatedAt });
                b.HasIndex(j => j.DeduplicationKey)
                    .IsUnique()
                    .HasFilter(DatabaseProvider.NotNullIndexFilter(this, nameof(BackgroundJobRun.DeduplicationKey)));
            });

            builder.Entity<AuditLog>(b =>
            {
                b.ToTable("AuditLogs");
                b.HasKey(a => a.Id);

                b.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
                b.Property(a => a.EntityId).HasMaxLength(64).IsRequired();
                b.Property(a => a.EntityDescription).HasMaxLength(256);
                b.Property(a => a.ActorEmail).HasMaxLength(256);
                b.Property(a => a.ActorName).HasMaxLength(256);
                b.Property(a => a.IpAddress).HasMaxLength(64);
                b.Property(a => a.ChangesJson).HasColumnType(jsonType);

                b.HasIndex(a => new { a.OccurredAt, a.Id }).IsDescending();
                b.HasIndex(a => new { a.EntityType, a.EntityId });
                b.HasIndex(a => new { a.ActorUserId, a.OccurredAt });
                b.HasIndex(a => new { a.Action, a.OccurredAt });
            });
        }

        public override int SaveChanges()
        {
            ApplyAuditing();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditing();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditing()
        {
            var now = DateTime.UtcNow;
            var userId = _currentUserContext.UserIdOrNull;

            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedBy = userId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = userId;

                        if (entry.Entity.IsDeleted && entry.Entity.DeletedAt is null)
                        {
                            entry.Entity.DeletedAt = now;
                            entry.Entity.DeletedBy = userId;
                        }
                        break;
                }
            }
        }
    }
}
