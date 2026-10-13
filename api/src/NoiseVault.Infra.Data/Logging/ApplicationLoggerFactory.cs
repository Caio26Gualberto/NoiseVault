using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Infra.Data.Logging
{
    public sealed class ApplicationLoggerFactory : IApplicationLoggerFactory
    {
        private readonly ILogContextStore _store;
        private readonly IApplicationLogWriter _writer;
        private readonly ApplicationLogMetadata _metadata;

        public ApplicationLoggerFactory(
            ILogContextStore store,
            IApplicationLogWriter writer,
            ApplicationLogMetadata metadata)
        {
            _store = store;
            _writer = writer;
            _metadata = metadata;
        }

        public IApplicationLogger Create(LogContext context)
            => new ApplicationLogger(context, _store, _writer, _metadata);
    }
}
