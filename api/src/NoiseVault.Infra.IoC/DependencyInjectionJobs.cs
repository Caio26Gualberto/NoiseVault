using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NoiseVault.Workers.Wrappers.Recurring.Discography;
using NoiseVault.Workers.Wrappers.Recurring.Media;
using NoiseVault.Workers.Wrappers.Recurring.Notifications;

namespace NoiseVault.Infra.IoC
{
    public static class DependencyInjectionJobs
    {
        public static WebApplication RegisterJobs(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            scope.ServiceProvider.GetRequiredService<MediaRecurringJobsWrapper>().Register();
            scope.ServiceProvider.GetRequiredService<NotificationsRecurringJobsWrapper>().Register();
            scope.ServiceProvider.GetRequiredService<DiscographyRecurringJobsWrapper>().Register();

            return app;
        }
    }
}
