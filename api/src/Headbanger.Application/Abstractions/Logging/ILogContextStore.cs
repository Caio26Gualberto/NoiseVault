namespace Headbanger.Application.Abstractions.Logging
{
    public interface ILogContextStore
    {
        LogContext? CurrentContext { get; }

        string? Operation { get; }

        IReadOnlyDictionary<string, object?> Values { get; }

        void Activate(LogContext context);

        void SetOperation(string operation);

        void Set(string key, object? value);

        IReadOnlyDictionary<string, object?> CreateSnapshot();
    }
}
