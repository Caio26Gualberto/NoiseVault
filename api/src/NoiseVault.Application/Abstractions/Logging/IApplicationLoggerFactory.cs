namespace NoiseVault.Application.Abstractions.Logging
{
    public interface IApplicationLoggerFactory
    {
        IApplicationLogger Create(LogContext context);
    }
}
