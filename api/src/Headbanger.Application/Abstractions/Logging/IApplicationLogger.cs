namespace Headbanger.Application.Abstractions.Logging
{
    public interface IApplicationLogger
    {
        void SetOperation(string operation);

        void Set(string key, object? value);

        void Info(
            string eventName,
            string message,
            int? userId = null,
            object? extra = null);

        void Warning(
            string eventName,
            string message,
            int? userId = null,
            object? extra = null);

        void Error(
            string eventName,
            string message,
            Exception? exception = null,
            int? userId = null,
            object? extra = null);

        void Critical(
            string eventName,
            string message,
            Exception exception,
            int? userId = null,
            object? extra = null);
    }
}
