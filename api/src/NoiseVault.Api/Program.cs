using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using NoiseVault.Api.Authentication;
using NoiseVault.Api.Authorization;
using NoiseVault.Api.Middlewares;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Api.Realtime;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Realtime;
using NoiseVault.Infra.Data.Context;
using NoiseVault.Infra.Data.Context.Seeding;
using NoiseVault.Infra.IoC;

const string CorsPolicy = "AllowConfiguredFrontends";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHangfire(config =>
{
    if (DatabaseProvider.Resolve(builder.Configuration) == EDatabaseProvider.SqlServer)
        config.UseSqlServerStorage(connectionString);
    else
        config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "noisevault-main";
    options.Queues = new[] { "notifications", "default" };
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "noisevault-media";
    options.Queues = new[] { "media" };
    options.WorkerCount = 2;
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<PasswordChangeRequiredFilter>();
});
builder.Services.AddApiRateLimiting();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IUserIdProvider, UserIdClaimProvider>();
builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe apenas o token JWT (sem o prefixo 'Bearer ').",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "Cors:AllowedOrigins não configurado. Defina Cors__AllowedOrigins__0 com a URL do frontend.");

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();
builder.Services.AddInfra(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

JobStorage.Current = app.Services.GetRequiredService<JobStorage>();

if (!app.Environment.IsProduction())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter()]
    });
}

app.RegisterJobs();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    var seeder = scope.ServiceProvider.GetRequiredService<SeedData>();
    await seeder.SeedAsync(includeAdminAccount: !app.Environment.IsProduction());
}

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationsHub>(NotificationsHub.Path);

app.Run();
