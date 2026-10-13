using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NoiseVault.Infra.Data.Identity
{
    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
    }

    public class PasswordResetTokenProvider : DataProtectorTokenProvider<ApplicationUser>
    {
        public const string ProviderName = "PasswordReset";

        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<PasswordResetTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
