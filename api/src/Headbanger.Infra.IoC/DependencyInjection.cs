using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Headbanger.Application.Abstractions.Authorization;
using Headbanger.Application.Abstractions.Jobs;
using Headbanger.Application.Abstractions.Logging;
using Headbanger.Application.Abstractions.Repository;
using Headbanger.Application.Abstractions.Repository.UnitOfWork;
using Headbanger.Application.AuditLogs;
using Headbanger.Application.AuditLogs.Interfaces;
using Headbanger.Application.Auth;
using Headbanger.Application.Auth.Interfaces;
using Headbanger.Application.Authorizations;
using Headbanger.Application.Authorizations.Interfaces;
using Headbanger.Application.BackgroundJobs;
using Headbanger.Application.BackgroundJobs.Interfaces;
using Headbanger.Application.Common.Time;
using Headbanger.Application.Interfaces;
using Headbanger.Application.Profiles;
using Headbanger.Application.Profiles.Interfaces;
using Headbanger.Application.Users;
using Headbanger.Application.Users.Interfaces;
using Headbanger.Domain.Constants;
using Headbanger.Domain.Interfaces.Authenticate;
using Headbanger.Infra.Data.Auditing;
using Headbanger.Infra.Data.Cache;
using Headbanger.Infra.Data.Context;
using Headbanger.Infra.Data.Context.Seeding;
using Headbanger.Infra.Data.Email;
using Headbanger.Infra.Data.Identity;
using Headbanger.Infra.Data.Identity.AuthenticateService;
using Headbanger.Infra.Data.Identity.Authorization;
using Headbanger.Infra.Data.Logging;
using Headbanger.Infra.Data.Repositories;
using Headbanger.Infra.Data.Repositories.AuditLogs;
using Headbanger.Infra.Data.Repositories.BackgroundJobRuns;
using Headbanger.Infra.Data.Repositories.UnitOfWork;
using Headbanger.Infra.Data.Storage;
using Headbanger.Workers.Dispatch;
using Headbanger.Workers.Monitoring;
using Headbanger.Workers.Triggers.Enqueued.BackgroundJobs;

namespace Headbanger.Infra.IoC
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
                Console.WriteLine("[Headbanger] Email:Provider não é Smtp — e-mails serão apenas logados (EMAIL_STUBBED).");
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
