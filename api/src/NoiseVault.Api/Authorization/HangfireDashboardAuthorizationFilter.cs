using Hangfire.Dashboard;

namespace NoiseVault.Api.Authorization
{
    public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}
