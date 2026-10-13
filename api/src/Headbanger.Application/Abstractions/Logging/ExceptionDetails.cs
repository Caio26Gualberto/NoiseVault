using System.Text;

namespace Headbanger.Application.Abstractions.Logging
{
    public static class ExceptionDetails
    {
        private const int MaxDepth = 5;
        private const int MaxStackTraceLength = 8000;

        public static string? CausedBy(Exception? exception)
        {
            if (exception?.InnerException is null)
                return null;

            var builder = new StringBuilder();
            var inner = exception.InnerException;
            var depth = 0;

            while (inner is not null && depth < MaxDepth)
            {
                if (builder.Length > 0)
                    builder.Append(" <- ");

                builder.Append(inner.GetType().FullName).Append(": ").Append(inner.Message);

                inner = inner.InnerException;
                depth++;
            }

            if (inner is not null)
                builder.Append(" <- ...");

            return builder.ToString();
        }

        public static string? StackTrace(Exception? exception)
        {
            var stackTrace = exception?.StackTrace;

            if (string.IsNullOrEmpty(stackTrace) || stackTrace.Length <= MaxStackTraceLength)
                return stackTrace;

            return stackTrace[..MaxStackTraceLength] + " …[truncated]";
        }
    }
}
