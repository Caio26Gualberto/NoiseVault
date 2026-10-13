using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            // External services
            var emailSettings = EmailSettings.FromConfiguration(config);
            services.AddSingleton(emailSettings);

            if (emailSettings.UsesSmtp)
                services.AddScoped<IEmailSender, SmtpEmailSender>();
            else
            {
                Console.WriteLine("[Noise Vault] Email:Provider não é Smtp — e-mails serão apenas logados (EMAIL_STUBBED).");
                services.AddScoped<IEmailSender, ConsoleEmailSender>();
            }

            services.AddScoped<IEmailService, EmailService>();

            services.AddFileStorage(config);

            // Jobs
            services.AddScoped<IRecurringJobReader, RecurringJobReader>();
            services.AddScoped<IEnqueuedJobDispatcher, EnqueuedJobDispatcher>();
            services.AddScoped<IBackgroundJobAppService, BackgroundJobAppService>();
            services.AddScoped<BackgroundJobsEnqueuedTrigger>();

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IBackgroundJobRunRepository, BackgroundJobRunRepository>();

            return services;
        }
    }
}
