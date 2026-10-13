namespace NoiseVault.Domain.Interfaces.Authenticate
{
    public enum EUserCreationFailure
    {
        Unknown = 0,
        EmailAlreadyInUse = 1,
        InvalidEmail = 2,
        WeakPassword = 3,
    }

    public sealed record UserCreationResult(
        bool Succeeded,
        int UserId,
        string Email,
        EUserCreationFailure Failure,
        IReadOnlyList<string> Errors)
    {
        public static UserCreationResult Ok(int userId, string email) =>
            new(true, userId, email, EUserCreationFailure.Unknown, []);

        public static UserCreationResult Fail(
            EUserCreationFailure failure,
            IReadOnlyList<string> errors) =>
            new(false, 0, string.Empty, failure, errors);
    }
}
