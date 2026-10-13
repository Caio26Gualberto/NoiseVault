using Headbanger.Application.Abstractions.Logging;

namespace Headbanger.Infra.Data.Logging
{
    public sealed class ApplicationLogger : IApplicationLogger
    {
        private readonly LogContext _context;
        private readonly ILogContextStore _store;
        private readonly IApplicationLogWriter _writer;
        private readonly ApplicationLogMetadata _metadata;

        public ApplicationLogger(
            LogContext context,
            ILogContextStore store,
            IApplicationLogWriter writer,
            ApplicationLogMetadata metadata)
        {
            _context = context;
            _store = store;
            _writer = writer;
            _metadata = metadata;
        }

        public void SetOperation(string operation)
        {
            _store.Activate(_context);
            _store.SetOperation(operation);
        }

        public void Set(string key, object? value)
        {
            _store.Activate(_context);
            _store.Set(key, value);
        }

        public void Info(string eventName, string message, int? userId = null, object? extra = null)
            => Write(ApplicationLogLevel.Information, eventName, message, userId, extra, exception: null);

        public void Warning(string eventName, string message, int? userId = null, object? extra = null)
            => Write(ApplicationLogLevel.Warning, eventName, message, userId, extra, exception: null);

        public void Error(string eventName, string message, Exception? exception = null, int? userId = null, object? extra = null)
            => Write(ApplicationLogLevel.Error, eventName, message, userId, extra, exception);

        public void Critical(string eventName, string message, Exception exception, int? userId = null, object? extra = null)
            => Write(ApplicationLogLevel.Critical, eventName, message, userId, extra, exception);

        private void Write(
            ApplicationLogLevel level,
            string eventName,
            string message,
            int? userId,
            object? extra,
            Exception? exception)
        {
            _store.Activate(_context);

            var data = _store.CreateSnapshot();

            var entry = new ApplicationLogEntry
            {
                Level = level,
                Context = _context,
                EventName = eventName,
                Message = message,
                Operation = _store.Operation,
                Application = _metadata.Application,
                Environment = _metadata.Environment,
                UserId = userId,
                Data = data.Count == 0 ? null : data,
                Extra = extra,
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message,
                CausedBy = ExceptionDetails.CausedBy(exception),
                StackTrace = ExceptionDetails.StackTrace(exception),
            };

            _writer.Write(entry);
        }
    }
}
