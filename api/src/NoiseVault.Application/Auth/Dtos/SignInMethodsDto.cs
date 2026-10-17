namespace NoiseVault.Application.Auth.Dtos
{
    public class SignInMethodsDto
    {
        public bool HasPassword { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<int> Providers { get; set; } = [];
    }
}
