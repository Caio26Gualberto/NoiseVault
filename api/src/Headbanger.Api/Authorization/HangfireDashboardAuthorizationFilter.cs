using Hangfire.Dashboard;

namespace Headbanger.Api.Authorization
{
    public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}
