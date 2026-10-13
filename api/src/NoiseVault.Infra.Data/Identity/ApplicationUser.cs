using Microsoft.AspNetCore.Identity;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Infra.Data.Identity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public bool IsNeededChangePassword { get; set; } = false;
    }
}
