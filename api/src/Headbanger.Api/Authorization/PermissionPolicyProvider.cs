using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Headbanger.Domain.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Collections.Concurrent;

namespace Headbanger.Api.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;
        private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policies = new();

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!PermissionCatalog.Exists(policyName))
                return _fallback.GetPolicyAsync(policyName);

            var policy = _policies.GetOrAdd(policyName, name =>
                new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(name))
                    .Build());

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
