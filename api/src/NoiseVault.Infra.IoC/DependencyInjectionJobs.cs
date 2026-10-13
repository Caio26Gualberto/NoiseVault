using Microsoft.AspNetCore.Builder;

namespace NoiseVault.Infra.IoC
{
    public static class DependencyInjectionJobs
    {
        public static WebApplication RegisterJobs(this WebApplication app)
        {
            return app;
        }
    }
}
