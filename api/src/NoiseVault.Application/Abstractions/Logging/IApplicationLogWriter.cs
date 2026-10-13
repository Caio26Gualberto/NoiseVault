namespace NoiseVault.Application.Abstractions.Logging
{
    public interface IApplicationLogWriter
    {
        void Write(ApplicationLogEntry entry);
    }
}
