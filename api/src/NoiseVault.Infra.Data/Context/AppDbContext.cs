using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
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
        public DbSet<City> Cities => Set<City>();
        public DbSet<Band> Bands => Set<Band>();
        public DbSet<BandGenre> BandGenres => Set<BandGenre>();
        public DbSet<BandMember> BandMembers => Set<BandMember>();
        public DbSet<BandInvitation> BandInvitations => Set<BandInvitation>();
        public DbSet<MediaUpload> MediaUploads => Set<MediaUpload>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<PostMedia> PostMedia => Set<PostMedia>();
        public DbSet<PhotoTag> PhotoTags => Set<PhotoTag>();
        public DbSet<MusicianProfile> MusicianProfiles => Set<MusicianProfile>();
        public DbSet<MusicianInstrument> MusicianInstruments => Set<MusicianInstrument>();
        public DbSet<MusicianGenre> MusicianGenres => Set<MusicianGenre>();
        public DbSet<MusicianAvailability> MusicianAvailability => Set<MusicianAvailability>();
        public DbSet<MusicianSampleLink> MusicianSampleLinks => Set<MusicianSampleLink>();
        public DbSet<BandOpening> BandOpenings => Set<BandOpening>();
        public DbSet<BandOpeningInterest> BandOpeningInterests => Set<BandOpeningInterest>();
        public DbSet<PostLike> PostLikes => Set<PostLike>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventGenre> EventGenres => Set<EventGenre>();
        public DbSet<EventLineupEntry> EventLineupEntries => Set<EventLineupEntry>();
        public DbSet<EventInterest> EventInterests => Set<EventInterest>();
        public DbSet<EventComment> EventComments => Set<EventComment>();
        public DbSet<ArchiveEntry> ArchiveEntries => Set<ArchiveEntry>();
        public DbSet<ArchiveBandEntry> ArchiveBandEntries => Set<ArchiveBandEntry>();
        public DbSet<ArchiveMedia> ArchiveMedia => Set<ArchiveMedia>();
        public DbSet<ArchiveLike> ArchiveLikes => Set<ArchiveLike>();
        public DbSet<ArchiveAttendance> ArchiveAttendances => Set<ArchiveAttendance>();
        public DbSet<ArchiveComment> ArchiveComments => Set<ArchiveComment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<SupportTicketTag> SupportTicketTags => Set<SupportTicketTag>();
        public DbSet<SupportTicketArea> SupportTicketAreas => Set<SupportTicketArea>();
        public DbSet<SupportTicketMedia> SupportTicketMedia => Set<SupportTicketMedia>();
        public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();
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
                b.Property(u => u.EmailOnBandInvite).HasDefaultValue(true);
                b.Property(u => u.CityName).HasMaxLength(80);

                b.HasOne(u => u.City)
                    .WithMany()
                    .HasForeignKey(u => u.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(u => u.Genres)
                    .WithOne()
                    .HasForeignKey(g => g.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<City>(b =>
            {
                b.ToTable("Cities");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();

                b.Property(x => x.Name).HasMaxLength(80).IsRequired();
                b.Property(x => x.Uf).HasMaxLength(2).IsRequired();
                b.Property(x => x.SearchName).HasMaxLength(80).IsRequired();

                b.HasIndex(x => x.SearchName);
            });

            builder.Entity<UserGenre>(b =>
            {
                b.ToTable("UserGenres");
                b.HasKey(g => new { g.UserId, g.Genre });
            });

            builder.Entity<Band>(b =>
            {
                b.ToTable("Bands");
                b.HasKey(x => x.Id);

                b.Property(x => x.Name).HasMaxLength(100).IsRequired();
                b.Property(x => x.Slug).HasMaxLength(60).IsRequired();
                b.HasIndex(x => x.Slug).IsUnique();

                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.InstagramUrl).HasMaxLength(300);
                b.Property(x => x.YoutubeUrl).HasMaxLength(300);
                b.Property(x => x.SpotifyUrl).HasMaxLength(300);
                b.Property(x => x.AvatarKey).HasMaxLength(200);
                b.Property(x => x.BannerKey).HasMaxLength(200);
                b.Property(x => x.BannerPosition).HasDefaultValue(Band.DefaultBannerPosition);
                b.Property(x => x.CityName).HasMaxLength(80);

                b.HasOne(x => x.City)
                    .WithMany()
                    .HasForeignKey(x => x.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(x => x.Genres)
                    .WithOne()
                    .HasForeignKey(g => g.BandId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.CreatedAt);
            });

            builder.Entity<BandGenre>(b =>
            {
                b.ToTable("BandGenres");
                b.HasKey(g => new { g.BandId, g.Genre });
            });

            builder.Entity<BandMember>(b =>
            {
                b.ToTable("BandMembers");
                b.HasKey(x => x.Id);

                b.Property(x => x.Instrument).HasMaxLength(60);

                b.HasOne(x => x.Band)
                    .WithMany(x => x.Members)
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.BandId, x.UserId }).IsUnique();
                b.HasIndex(x => x.UserId);
            });

            builder.Entity<BandInvitation>(b =>
            {
                b.ToTable("BandInvitations");
                b.HasKey(x => x.Id);

                b.Property(x => x.Instrument).HasMaxLength(60);

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.InvitedUser)
                    .WithMany()
                    .HasForeignKey(x => x.InvitedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.InvitedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.InvitedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.BandId, x.InvitedUserId })
                    .IsUnique()
                    .HasFilter(DatabaseProvider.ValueIndexFilter(this, nameof(BandInvitation.Status), (int)EBandInvitationStatus.Pending));

                b.HasIndex(x => new { x.InvitedUserId, x.Status });
            });

            builder.Entity<MediaUpload>(b =>
            {
                b.ToTable("MediaUploads");
                b.HasKey(x => x.Id);

                b.Property(x => x.StorageKey).HasMaxLength(200);
                b.Property(x => x.OriginalKey).HasMaxLength(200);
                b.Property(x => x.MultipartUploadId).HasMaxLength(1024);
                b.Property(x => x.PosterKey).HasMaxLength(200);
                b.Property(x => x.FailureReason).HasMaxLength(60);

                b.HasOne(x => x.Owner)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.Status, x.ExpiresAt });
                b.HasIndex(x => new { x.OwnerUserId, x.Status });
            });

            builder.Entity<Post>(b =>
            {
                b.ToTable("Posts");
                b.HasKey(x => x.Id);

                b.Property(x => x.Content).HasMaxLength(5000).IsRequired();

                b.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Event)
                    .WithMany()
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.EventId)
                    .IsUnique()
                    .HasFilter(DatabaseProvider.NotNullIndexFilter(this, nameof(Post.EventId)));

                b.HasOne(x => x.ArchiveEntry)
                    .WithMany()
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.ArchiveEntryId)
                    .IsUnique()
                    .HasFilter(DatabaseProvider.NotNullIndexFilter(this, nameof(Post.ArchiveEntryId)));

                b.HasIndex(x => x.PublishedAt);
                b.HasIndex(x => new { x.AuthorUserId, x.PublishedAt });
                b.HasIndex(x => new { x.BandId, x.PublishedAt });

                b.HasOne(x => x.BandOpening)
                    .WithMany()
                    .HasForeignKey(x => x.BandOpeningId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.BandOpeningId)
                    .IsUnique()
                    .HasFilter(DatabaseProvider.NotNullIndexFilter(this, nameof(Post.BandOpeningId)));
            });

            builder.Entity<PostMedia>(b =>
            {
                b.ToTable("PostMedia");
                b.HasKey(x => x.Id);

                b.HasOne(x => x.Post)
                    .WithMany(x => x.Media)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.MediaUpload)
                    .WithOne(x => x.Attachment)
                    .HasForeignKey<PostMedia>(x => x.MediaUploadId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.MediaUploadId).IsUnique();
                b.HasIndex(x => new { x.PostId, x.Order });
            });

            builder.Entity<PostLike>(b =>
            {
                b.ToTable("PostLikes");
                b.HasKey(x => new { x.PostId, x.UserId });

                b.HasOne<Post>()
                    .WithMany(x => x.Likes)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.UserId);
            });

            builder.Entity<Comment>(b =>
            {
                b.ToTable("Comments");
                b.HasKey(x => x.Id);

                b.Property(x => x.Text).HasMaxLength(2000).IsRequired();

                b.HasOne(x => x.Post)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.PostId, x.CreatedAt });
            });

            builder.Entity<Event>(b =>
            {
                b.ToTable("Events");
                b.HasKey(x => x.Id);

                b.Property(x => x.Title).HasMaxLength(120).IsRequired();
                b.Property(x => x.Description).HasMaxLength(5000);
                b.Property(x => x.Venue).HasMaxLength(120).IsRequired();
                b.Property(x => x.CityName).HasMaxLength(80);

                b.HasOne(x => x.City)
                    .WithMany()
                    .HasForeignKey(x => x.CityId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.Property(x => x.TicketsUrl).HasMaxLength(500);
                b.Property(x => x.Price).HasPrecision(10, 2);
                b.Property(x => x.BannerKey).HasMaxLength(200);
                b.Property(x => x.BannerPosition).HasDefaultValue(Event.DefaultBannerPosition);

                b.HasOne(x => x.Organizer)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(x => x.Genres)
                    .WithOne()
                    .HasForeignKey(g => g.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.EndsAtOrDefault, x.StartsAt });
                b.HasIndex(x => new { x.BandId, x.StartsAt });
                b.HasIndex(x => x.OrganizerUserId);
            });

            builder.Entity<EventGenre>(b =>
            {
                b.ToTable("EventGenres");
                b.HasKey(g => new { g.EventId, g.Genre });
            });

            builder.Entity<EventLineupEntry>(b =>
            {
                b.ToTable("EventLineupEntries");
                b.HasKey(x => x.Id);

                b.Property(x => x.Name).HasMaxLength(100);

                b.HasOne(x => x.Event)
                    .WithMany(x => x.Lineup)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.EventId, x.Order });
                b.HasIndex(x => x.BandId);
            });

            builder.Entity<EventInterest>(b =>
            {
                b.ToTable("EventInterests");
                b.HasKey(x => new { x.EventId, x.UserId });

                b.HasOne<Event>()
                    .WithMany(x => x.Interests)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.UserId);
            });

            builder.Entity<EventComment>(b =>
            {
                b.ToTable("EventComments");
                b.HasKey(x => x.Id);

                b.Property(x => x.Text).HasMaxLength(2000).IsRequired();

                b.HasOne(x => x.Event)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.EventId, x.CreatedAt });
            });

            builder.Entity<ArchiveEntry>(b =>
            {
                b.ToTable("ArchiveEntries");
                b.HasKey(x => x.Id);

                b.Property(x => x.Title).HasMaxLength(150).IsRequired();
                b.Property(x => x.Description).HasMaxLength(10000);
                b.Property(x => x.Venue).HasMaxLength(120);
                b.Property(x => x.CityName).HasMaxLength(80);
                b.Property(x => x.RejectionReason).HasMaxLength(500);

                b.HasOne(x => x.City)
                    .WithMany()
                    .HasForeignKey(x => x.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.SortDate);
                b.HasIndex(x => x.Year);
                b.HasIndex(x => new { x.Status, x.CreatedAt });
                b.HasIndex(x => new { x.AuthorUserId, x.Status });
            });

            builder.Entity<ArchiveBandEntry>(b =>
            {
                b.ToTable("ArchiveBandEntries");
                b.HasKey(x => x.Id);

                b.Property(x => x.Name).HasMaxLength(100);

                b.HasOne(x => x.ArchiveEntry)
                    .WithMany(x => x.Bands)
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.ArchiveEntryId, x.Order });
                b.HasIndex(x => x.BandId);
            });

            builder.Entity<MusicianProfile>(b =>
            {
                b.ToTable("MusicianProfiles");
                b.HasKey(x => x.Id);

                b.Property(x => x.Influences).HasMaxLength(300);
                b.Property(x => x.PhotoKey).HasMaxLength(200);

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.UserId)
                    .IsUnique()
                    .HasFilter(DatabaseProvider.SoftDeleteIndexFilter(this));

                b.HasIndex(x => new { x.IsLookingForBand, x.LookingSince });
            });

            builder.Entity<MusicianInstrument>(b =>
            {
                b.ToTable("MusicianInstruments");
                b.HasKey(x => x.Id);

                b.Property(x => x.OtherName).HasMaxLength(60);

                b.HasOne<MusicianProfile>()
                    .WithMany(x => x.Instruments)
                    .HasForeignKey(x => x.MusicianProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.Instrument, x.MusicianProfileId });
            });

            builder.Entity<MusicianGenre>(b =>
            {
                b.ToTable("MusicianGenres");
                b.HasKey(g => new { g.MusicianProfileId, g.Genre });

                b.HasOne<MusicianProfile>()
                    .WithMany(x => x.Genres)
                    .HasForeignKey(x => x.MusicianProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<MusicianAvailability>(b =>
            {
                b.ToTable("MusicianAvailability");
                b.HasKey(a => new { a.MusicianProfileId, a.Day, a.Period });

                b.HasOne<MusicianProfile>()
                    .WithMany(x => x.Availability)
                    .HasForeignKey(x => x.MusicianProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<MusicianSampleLink>(b =>
            {
                b.ToTable("MusicianSampleLinks");
                b.HasKey(x => x.Id);

                b.Property(x => x.Url).HasMaxLength(300).IsRequired();

                b.HasOne<MusicianProfile>()
                    .WithMany(x => x.SampleLinks)
                    .HasForeignKey(x => x.MusicianProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<BandOpening>(b =>
            {
                b.ToTable("BandOpenings");
                b.HasKey(x => x.Id);

                b.Property(x => x.OtherName).HasMaxLength(60);
                b.Property(x => x.Description).HasMaxLength(500).IsRequired();

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.BandId, x.Status });
                b.HasIndex(x => new { x.Status, x.ExpiresAt });
            });

            builder.Entity<BandOpeningInterest>(b =>
            {
                b.ToTable("BandOpeningInterests");
                b.HasKey(x => x.Id);

                b.Property(x => x.Message).HasMaxLength(300);

                b.HasOne(x => x.BandOpening)
                    .WithMany(x => x.Interests)
                    .HasForeignKey(x => x.BandOpeningId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne<BandInvitation>()
                    .WithMany()
                    .HasForeignKey(x => x.BandInvitationId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.BandOpeningId, x.UserId })
                    .IsUnique()
                    .HasFilter(DatabaseProvider.SoftDeleteIndexFilter(this));

                b.HasIndex(x => x.UserId);
            });

            builder.Entity<PhotoTag>(b =>
            {
                b.ToTable("PhotoTags");
                b.HasKey(x => x.Id);

                b.Property(x => x.Name).HasMaxLength(80);

                b.HasOne(x => x.MediaUpload)
                    .WithMany()
                    .HasForeignKey(x => x.MediaUploadId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.TaggedUser)
                    .WithMany()
                    .HasForeignKey(x => x.TaggedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.TaggedBy)
                    .WithMany()
                    .HasForeignKey(x => x.TaggedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.MediaUploadId);
                b.HasIndex(x => new { x.MediaUploadId, x.TaggedUserId })
                    .IsUnique()
                    .HasFilter(DatabaseProvider.NotNullIndexFilter(this, nameof(PhotoTag.TaggedUserId)));
            });

            builder.Entity<ArchiveMedia>(b =>
            {
                b.ToTable("ArchiveMedia");
                b.HasKey(x => x.Id);

                b.Property(x => x.Caption).HasMaxLength(500);

                b.HasOne(x => x.ArchiveEntry)
                    .WithMany(x => x.Media)
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.MediaUpload)
                    .WithOne(x => x.ArchiveAttachment)
                    .HasForeignKey<ArchiveMedia>(x => x.MediaUploadId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.MediaUploadId).IsUnique();
                b.HasIndex(x => new { x.ArchiveEntryId, x.Order });
            });

            builder.Entity<ArchiveLike>(b =>
            {
                b.ToTable("ArchiveLikes");
                b.HasKey(x => new { x.ArchiveEntryId, x.UserId });

                b.HasOne<ArchiveEntry>()
                    .WithMany(x => x.Likes)
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ArchiveAttendance>(b =>
            {
                b.ToTable("ArchiveAttendances");
                b.HasKey(x => new { x.ArchiveEntryId, x.UserId });

                b.HasOne<ArchiveEntry>()
                    .WithMany(x => x.Attendances)
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.ArchiveEntryId, x.CreatedAt });
            });

            builder.Entity<ArchiveComment>(b =>
            {
                b.ToTable("ArchiveComments");
                b.HasKey(x => x.Id);

                b.Property(x => x.Text).HasMaxLength(2000).IsRequired();

                b.HasOne(x => x.ArchiveEntry)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.ArchiveEntryId, x.CreatedAt });
            });

            builder.Entity<Notification>(b =>
            {
                b.ToTable("Notifications");
                b.HasKey(x => x.Id);

                b.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Actor)
                    .WithMany()
                    .HasForeignKey(x => x.ActorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Post)
                    .WithMany()
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Event)
                    .WithMany()
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.ArchiveEntry)
                    .WithMany()
                    .HasForeignKey(x => x.ArchiveEntryId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Band)
                    .WithMany()
                    .HasForeignKey(x => x.BandId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.BandInvitation)
                    .WithMany()
                    .HasForeignKey(x => x.BandInvitationId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.SupportTicket)
                    .WithMany()
                    .HasForeignKey(x => x.SupportTicketId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.BandOpening)
                    .WithMany()
                    .HasForeignKey(x => x.BandOpeningId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.UserId, x.ReadAt, x.LastActivityAt });
                b.HasIndex(x => x.LastActivityAt);
            });

            builder.Entity<SupportTicket>(b =>
            {
                b.ToTable("SupportTickets");
                b.HasKey(x => x.Id);

                b.Property(x => x.Title).HasMaxLength(120).IsRequired();
                b.Property(x => x.Description).HasMaxLength(5000).IsRequired();
                b.Property(x => x.OriginPath).HasMaxLength(300);
                b.Property(x => x.UserAgent).HasMaxLength(500);
                b.Property(x => x.InternalNote).HasMaxLength(5000);

                b.HasOne(x => x.Reporter)
                    .WithMany()
                    .HasForeignKey(x => x.ReporterUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.ReporterUserId, x.LastActivityAt });
                b.HasIndex(x => new { x.Status, x.LastActivityAt });
            });

            builder.Entity<SupportTicketTag>(b =>
            {
                b.ToTable("SupportTicketTags");
                b.HasKey(x => new { x.SupportTicketId, x.Tag });

                b.HasOne<SupportTicket>()
                    .WithMany(x => x.Tags)
                    .HasForeignKey(x => x.SupportTicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SupportTicketArea>(b =>
            {
                b.ToTable("SupportTicketAreas");
                b.HasKey(x => new { x.SupportTicketId, x.Area });

                b.HasOne<SupportTicket>()
                    .WithMany(x => x.Areas)
                    .HasForeignKey(x => x.SupportTicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SupportTicketMedia>(b =>
            {
                b.ToTable("SupportTicketMedia");
                b.HasKey(x => x.Id);

                b.HasOne(x => x.SupportTicket)
                    .WithMany(x => x.Media)
                    .HasForeignKey(x => x.SupportTicketId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.MediaUpload)
                    .WithOne(x => x.SupportAttachment)
                    .HasForeignKey<SupportTicketMedia>(x => x.MediaUploadId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.MediaUploadId).IsUnique();
                b.HasIndex(x => new { x.SupportTicketId, x.Order });
            });

            builder.Entity<SupportTicketMessage>(b =>
            {
                b.ToTable("SupportTicketMessages");
                b.HasKey(x => x.Id);

                b.Property(x => x.Text).HasMaxLength(2000).IsRequired();

                b.HasOne(x => x.SupportTicket)
                    .WithMany(x => x.Messages)
                    .HasForeignKey(x => x.SupportTicketId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.SupportTicketId, x.CreatedAt });
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
