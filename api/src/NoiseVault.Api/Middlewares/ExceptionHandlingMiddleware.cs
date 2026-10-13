using System.Diagnostics;
using System.Net;
using NoiseVault.Api.ApiResponse;
using NoiseVault.Api.Diagnostics;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Api.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ILogContextStore logContextStore,
            IApplicationLogWriter logWriter,
            ApplicationLogMetadata logMetadata,
            ICurrentUser currentUser)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            var stopwatch = Stopwatch.StartNew();

            context.Response.Headers[RequestDescriptor.TraceIdHeader] = traceId;

            try
            {
                await _next(context);

                stopwatch.Stop();

                if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                    Log(
                        context, exception: null, traceId, stopwatch.ElapsedMilliseconds,
                        "REQUEST_FAILED", "Request failed without raising an exception",
                        logContextStore, logWriter, logMetadata, currentUser);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (!context.Response.HasStarted)
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                Log(
                    context, ex, traceId, stopwatch.ElapsedMilliseconds,
                    "UNHANDLED_EXCEPTION", "Unhandled exception",
                    logContextStore, logWriter, logMetadata, currentUser);

                if (context.Response.HasStarted)
                    throw;

                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Code = "UNEXPECTED_ERROR",
                    Message = "An unexpected error occurred.",
                    Data = null
                };

                await context.Response.WriteAsync(EnvelopeJson.Serialize(response));
            }
        }

        private static void Log(
            HttpContext context,
            Exception? exception,
            string traceId,
            long elapsedMs,
            string eventName,
            string message,
            ILogContextStore logContextStore,
            IApplicationLogWriter logWriter,
            ApplicationLogMetadata logMetadata,
            ICurrentUser currentUser)
        {
            try
            {
                var userId = currentUser.UserIdOrNull;
                var data = new Dictionary<string, object?>(logContextStore.CreateSnapshot())
                {
                    ["TraceId"] = traceId,
                    ["ElapsedMs"] = elapsedMs,
                };

                RequestDescriptor.Describe(context, data);

                if (userId.HasValue)
                    data["AuthenticatedUserId"] = userId.Value;

                if (context.Items.TryGetValue(RequestDescriptor.ErrorCodeItemKey, out var errorCode))
                    data["ErrorCode"] = errorCode;

                logWriter.Write(new ApplicationLogEntry
                {
                    Level = ApplicationLogLevel.Error,
                    Context = logContextStore.CurrentContext ?? LogContext.Api,
                    EventName = eventName,
                    Message = message,
                    Operation = logContextStore.Operation,
                    Application = logMetadata.Application,
                    Environment = logMetadata.Environment,
                    UserId = userId,
                    Data = data,
                    ExceptionType = exception?.GetType().FullName,
                    ExceptionMessage = exception?.Message,
                    CausedBy = ExceptionDetails.CausedBy(exception),
                    StackTrace = ExceptionDetails.StackTrace(exception),
                });
            }
            catch (Exception loggingFailure)
            {
                Console.Error.WriteLine(
                    $"LOGGER_FAILURE {eventName} trace={traceId} " +
                    $"original={exception?.GetType().Name}: {exception?.Message} " +
                    $"loggerError={loggingFailure.GetType().Name}: {loggingFailure.Message}");
            }
        }
    }
}
