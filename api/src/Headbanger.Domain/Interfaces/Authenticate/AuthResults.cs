namespace Headbanger.Domain.Interfaces.Authenticate
{
    public enum EEmailConfirmationResult
    {
        Confirmed = 0,
        AlreadyConfirmed = 1,
        Invalid = 2,
    }

    public enum EPasswordResetResult
    {
        Success = 0,
        InvalidToken = 1,
        WeakPassword = 2,
    }

    public enum EPasswordChangeResult
    {
        Success = 0,
        InvalidCurrentPassword = 1,
        WeakPassword = 2,
        NotFound = 3,
    }
}
