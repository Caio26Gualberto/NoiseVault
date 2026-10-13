namespace NoiseVault.Application.Abstractions.Authentication
{
    public interface ICurrentUser
    {
        int UserId { get; }
        string Email { get; }
        string Name { get; }
        bool IsAuthenticated { get; }

        string? Role { get; }

        int? UserIdOrNull { get; }

        string? IpAddress { get; }
    }
}
