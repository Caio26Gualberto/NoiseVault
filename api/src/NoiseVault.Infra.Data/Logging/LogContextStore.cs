using System.Collections.Concurrent;
using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Infra.Data.Logging
{
    public sealed class LogContextStore : ILogContextStore
    {
        private readonly ConcurrentDictionary<string, object?> _values = new(StringComparer.Ordinal);
        private readonly Lock _gate = new();

        private LogContext? _currentContext;
        private string? _operation;

        public LogContext? CurrentContext
        {
            get
            {
                lock (_gate)
                    return _currentContext;
            }
        }

        public string? Operation
        {
            get
            {
                lock (_gate)
                    return _operation;
            }
        }

        public IReadOnlyDictionary<string, object?> Values => CreateSnapshot();

        public void Activate(LogContext context)
        {
            lock (_gate)
                _currentContext = context;
        }

        public void SetOperation(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
                return;

            lock (_gate)
                _operation = operation;
        }

        public void Set(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _values[key] = value;
        }

        public IReadOnlyDictionary<string, object?> CreateSnapshot()
        {
            return new Dictionary<string, object?>(_values, StringComparer.Ordinal);
        }
    }
}
