using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NoiseVault.Workers.Wrappers.Recurring.Media;

namespace NoiseVault.Infra.IoC
{
    public static class DependencyInjectionJobs
    {
        public static WebApplication RegisterJobs(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            scope.ServiceProvider.GetRequiredService<MediaRecurringJobsWrapper>().Register();

            return app;
        }
    }
}
