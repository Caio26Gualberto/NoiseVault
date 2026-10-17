namespace NoiseVault.Application.Auth.Dtos
{
    public class ExternalLoginLinkedEmailDto
    {
        public string To { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
    }
}
