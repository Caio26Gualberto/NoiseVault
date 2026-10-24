using NoiseVault.Application.Seo;
using NoiseVault.Application.Seo.Interfaces;
using NoiseVault.Application.Share;
using NoiseVault.Application.Share.Interfaces;
using NoiseVault.Application.PhotoTags;
using NoiseVault.Application.PhotoTags.Interfaces;
using Amazon.SimpleEmailV2;
using NoiseVault.Infra.Data.Repositories.PhotoTags;
using NoiseVault.Application.Mentions;
using NoiseVault.Application.Mentions.Interfaces;
using NoiseVault.Application.Bands;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.BandOpenings;
using NoiseVault.Application.BandOpenings.Interfaces;
using NoiseVault.Application.Musicians;
using NoiseVault.Application.Musicians.Interfaces;
using NoiseVault.Infra.Data.Repositories.BandInvitations;
using NoiseVault.Infra.Data.Repositories.BandPartnerships;
using NoiseVault.Application.Archive;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Infra.Data.Repositories.Archive;
using NoiseVault.Infra.Data.Repositories.BandVerifications;
using NoiseVault.Infra.Data.Repositories.BandFollowers;
using NoiseVault.Infra.Data.Spotify;
using NoiseVault.Infra.Data.News;
using NoiseVault.Infra.Data.Repositories.News;
using NoiseVault.Application.News;
using NoiseVault.Application.News.Interfaces;
using NoiseVault.Workers.Triggers.Recurring.News;
using NoiseVault.Workers.Wrappers.Recurring.News;
using NoiseVault.Infra.Data.ExternalLogin;
using NoiseVault.Workers.Triggers.Recurring.Discography;
using NoiseVault.Workers.Wrappers.Recurring.Discography;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Infra.Data.Repositories.Notifications;
using NoiseVault.Application.Cities;
using NoiseVault.Application.Cities.Interfaces;
using NoiseVault.Infra.Data.Repositories.Cities;
using NoiseVault.Application.Support;
using NoiseVault.Application.Support.Interfaces;
using NoiseVault.Infra.Data.Repositories.Support;
using NoiseVault.Application.Events;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Infra.Data.Repositories.Events;
using NoiseVault.Application.Media;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Posts;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Infra.Data.Repositories.Posts;
using NoiseVault.Workers.Triggers.Enqueued.Media;
using NoiseVault.Workers.Triggers.Recurring.Media;
using NoiseVault.Workers.Wrappers.Recurring.Media;
using NoiseVault.Workers.Triggers.Enqueued.Notifications;
using NoiseVault.Workers.Triggers.Recurring.Notifications;
using NoiseVault.Workers.Wrappers.Recurring.Notifications;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.AuditLogs;
using NoiseVault.Application.AuditLogs.Interfaces;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Auth.Interfaces;
using NoiseVault.Application.Authorizations;
using NoiseVault.Application.Authorizations.Interfaces;
using NoiseVault.Application.BackgroundJobs;
using NoiseVault.Application.BackgroundJobs.Interfaces;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.Profiles.Interfaces;
using NoiseVault.Application.PublicProfiles;
using NoiseVault.Application.PublicProfiles.Interfaces;
using NoiseVault.Application.Users;
using NoiseVault.Application.Users.Interfaces;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Interfaces.Authenticate;
using NoiseVault.Infra.Data.Auditing;
using NoiseVault.Infra.Data.Cache;
using NoiseVault.Infra.Data.Context;
using NoiseVault.Infra.Data.Context.Seeding;
using NoiseVault.Infra.Data.Email;
using NoiseVault.Infra.Data.Identity;
using NoiseVault.Infra.Data.Identity.AuthenticateService;
using NoiseVault.Infra.Data.Identity.Authorization;
using NoiseVault.Infra.Data.Logging;
using NoiseVault.Infra.Data.Repositories;
using NoiseVault.Infra.Data.Repositories.AuditLogs;
using NoiseVault.Infra.Data.Repositories.BackgroundJobRuns;
using NoiseVault.Infra.Data.Repositories.UnitOfWork;
using NoiseVault.Infra.Data.Storage;
using NoiseVault.Workers.Dispatch;
using NoiseVault.Workers.Monitoring;
using NoiseVault.Workers.Triggers.Enqueued.BackgroundJobs;

namespace NoiseVault.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
        {
            AppTime.Configure(config["App:TimeZone"], config["App:WindowsTimeZone"]);

            // Logging
            services.AddSingleton(sp => new ApplicationLogMetadata(
                config["App:Name"] ?? ApplicationLogMetadata.DefaultApplication,
                sp.GetRequiredService<IHostEnvironment>().EnvironmentName));
            if (string.Equals(config["Logging:ApplicationLog:Format"], "Json", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IApplicationLogWriter, ConsoleApplicationLogWriter>();
            else
                services.AddSingleton<IApplicationLogWriter, TextApplicationLogWriter>();
            services.AddScoped<ILogContextStore, LogContextStore>();
            services.AddScoped<IApplicationLoggerFactory, ApplicationLoggerFactory>();

            // Data
            services.AddScoped<AuditSaveChangesInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
                DatabaseProvider.Configure(options, config)
                    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            // Identity
            services.AddDataProtection()
                .SetApplicationName("noisevault")
                .PersistKeysToDbContext<AppDbContext>();

            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(24));
            services.Configure<PasswordResetTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProvider.ProviderName;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<PasswordResetTokenProvider>(PasswordResetTokenProvider.ProviderName);

            var jwtSettings = config.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException(
                    "JWT:SecretKey não configurado. Defina a variável de ambiente JWT__SecretKey.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = JwtAudiences.Api,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = token;

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();
            services.AddHttpContextAccessor();

            // Cache
            services.AddHybridCache();
            services.AddScoped<ICacheClient, CacheClient>();

            // Auth e acesso
            services.AddScoped<IAuthenticateService, AuthenticateService>();
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IProfileAppService, ProfileAppService>();
            services.AddScoped<IPublicProfileAppService, PublicProfileAppService>();
            services.AddScoped<IPermissionDefinitionProvider, PermissionDefinitionProvider>();
            services.AddScoped<IPermissionChecker, PermissionChecker>();
            services.AddScoped<IPermissionManager, PermissionManager>();
            services.AddScoped<IRoleManagementService, RoleManagementService>();
            services.AddScoped<IAuthorizationAppService, AuthorizationAppService>();
            services.AddScoped<IUserAppService, UserAppService>();
            services.AddScoped<IAuditLogAppService, AuditLogAppService>();
            services.AddScoped<SeedData>();
            services.AddScoped<DevSeedMedia>();
            services.AddScoped<DevSeed>();

            // Bandas
            services.AddScoped<IBandAppService, BandAppService>();
            services.AddScoped<IBandInvitationAppService, BandInvitationAppService>();
            services.AddScoped<IBandPartnershipAppService, BandPartnershipAppService>();
            services.AddScoped<IBandVerificationAppService, BandVerificationAppService>();
            services.AddScoped<IBandDiscographyAppService, BandDiscographyAppService>();

            // Ensaio
            services.AddScoped<IMusicianAppService, MusicianAppService>();
            services.AddScoped<IBandOpeningAppService, BandOpeningAppService>();

            // Feed e mídia
            services.AddScoped<IMediaUploadAppService, MediaUploadAppService>();
            services.AddScoped<IMentionAppService, MentionAppService>();
            services.AddScoped<IPhotoTagAppService, PhotoTagAppService>();
            services.AddScoped<ISharePreviewAppService, SharePreviewAppService>();
            services.AddScoped<ISeoPageAppService, SeoPageAppService>();
            services.AddScoped<IPostAppService, PostAppService>();
            services.AddScoped<ICommentAppService, CommentAppService>();

            // Eventos
            services.AddScoped<IEventAppService, EventAppService>();
            services.AddScoped<INewsAppService, NewsAppService>();
            services.AddScoped<IEventCommentAppService, EventCommentAppService>();

            // Mural das antigas
            services.AddScoped<IArchiveAppService, ArchiveAppService>();
            services.AddScoped<IArchiveCommentAppService, ArchiveCommentAppService>();

            // Notificações
            services.AddScoped<INotificationAppService, NotificationAppService>();

            // Cidades
            services.AddScoped<ICityAppService, CityAppService>();

            // Suporte
            services.AddScoped<ISupportAppService, SupportAppService>();

            // External services
            var emailSettings = EmailSettings.FromConfiguration(config);
            services.AddSingleton(emailSettings);

            if (emailSettings.UsesSmtp)
                services.AddScoped<IEmailSender, SmtpEmailSender>();
            else if (emailSettings.UsesSes)
            {
                services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ => StorageRegistration.CreateSesClient(StorageSettings.FromConfiguration(config)));
                services.AddScoped<IEmailSender, SesEmailSender>();
            }
            else
            {
                Console.WriteLine("[Noise Vault] Email:Provider não é Smtp nem Ses — e-mails serão apenas logados (EMAIL_STUBBED).");
                services.AddScoped<IEmailSender, ConsoleEmailSender>();
            }

            services.AddScoped<IEmailService, EmailService>();

            services.AddFileStorage(config);

            var spotifySettings = SpotifySettings.FromConfiguration(config);
            services.AddSingleton(spotifySettings);
            services.AddSingleton<SpotifyTokenCache>();
            services.AddHttpClient<ISpotifyCatalog, SpotifyCatalogClient>(client => client.Timeout = TimeSpan.FromSeconds(20));

            if (!spotifySettings.IsConfigured)
                Console.WriteLine("[Noise Vault] Spotify:ClientId e Spotify:ClientSecret vazios — a importação de discografia responde SPOTIFY_NOT_CONFIGURED.");

            var newsSettings = WhiplashNewsSettings.FromConfiguration(config);
            services.AddSingleton(newsSettings);
            services.AddHttpClient<INewsSourceClient, WhiplashNewsClient>(client => client.Timeout = TimeSpan.FromSeconds(15));

            if (!newsSettings.Enabled)
                Console.WriteLine("[Noise Vault] News:Enabled = false — o job news:collect não busca notícias.");

            var externalLoginSettings = ExternalLoginSettings.FromConfiguration(config);
            services.AddSingleton(externalLoginSettings);
            services.AddHttpClient<IExternalIdentityService, ExternalIdentityService>(client => client.Timeout = TimeSpan.FromSeconds(10));

            if (!externalLoginSettings.IsGoogleConfigured)
                Console.WriteLine("[Noise Vault] Auth:Google:ClientId vazio — entrar com Google responde EXTERNAL_PROVIDER_DISABLED.");

            if (!externalLoginSettings.IsFacebookConfigured)
                Console.WriteLine("[Noise Vault] Auth:Facebook:AppId ou Auth:Facebook:AppSecret vazios — entrar com Facebook responde EXTERNAL_PROVIDER_DISABLED.");

            // Jobs
            services.AddScoped<IRecurringJobReader, RecurringJobReader>();
            services.AddScoped<IRecurringJobTrigger, RecurringJobTrigger>();
            services.AddScoped<IEnqueuedJobDispatcher, EnqueuedJobDispatcher>();
            services.AddScoped<IBackgroundJobAppService, BackgroundJobAppService>();
            services.AddScoped<BackgroundJobsEnqueuedTrigger>();
            services.AddScoped<MediaEnqueuedJobsTrigger>();
            services.AddScoped<MediaRecurringJobsTrigger>();
            services.AddScoped<MediaRecurringJobsWrapper>();
            services.AddScoped<NotificationsEnqueuedJobsTrigger>();
            services.AddScoped<NotificationsRecurringJobsTrigger>();
            services.AddScoped<NotificationsRecurringJobsWrapper>();
            services.AddScoped<DiscographyRecurringJobsTrigger>();
            services.AddScoped<DiscographyRecurringJobsWrapper>();
            services.AddScoped<NewsRecurringJobsTrigger>();
            services.AddScoped<NewsRecurringJobsWrapper>();

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IBackgroundJobRunRepository, BackgroundJobRunRepository>();
            services.AddScoped<IBandInvitationRepository, BandInvitationRepository>();
            services.AddScoped<IBandPartnershipRepository, BandPartnershipRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<INewsArticleRepository, NewsArticleRepository>();
            services.AddScoped<IArchiveRepository, ArchiveRepository>();
            services.AddScoped<IBandVerificationRepository, BandVerificationRepository>();
            services.AddScoped<IBandFollowerRepository, BandFollowerRepository>();
            services.AddScoped<IPhotoTagRepository, PhotoTagRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
            services.AddScoped<ICityRepository, CityRepository>();

            return services;
        }
    }
}
