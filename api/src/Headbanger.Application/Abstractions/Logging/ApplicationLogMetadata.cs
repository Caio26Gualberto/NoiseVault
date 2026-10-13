namespace Headbanger.Application.Abstractions.Logging
{
    public sealed record ApplicationLogMetadata(string Application, string Environment)
    {
        public const string DefaultApplication = "headbanger-api";
    }
}
