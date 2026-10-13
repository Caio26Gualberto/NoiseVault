using Microsoft.AspNetCore.Identity;
using Headbanger.Domain.Entities;

namespace Headbanger.Infra.Data.Identity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public bool IsNeededChangePassword { get; set; } = false;
    }
}
