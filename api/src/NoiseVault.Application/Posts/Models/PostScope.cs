namespace NoiseVault.Application.Posts.Models
{
    public sealed record PostScope(int? AuthorUserId = null, int? BandId = null)
    {
        public static readonly PostScope Everyone = new();
    }
}
