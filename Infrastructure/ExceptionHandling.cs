using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Infrastructure
{
    public static class CorrelationId
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemKey = "CorrelationId";

        public static string Get(HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(ItemKey, out var existing) && existing is string s && !string.IsNullOrWhiteSpace(s))
                return s;

            var fromHeader = httpContext.Request.Headers[HeaderName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(fromHeader))
            {
                httpContext.Items[ItemKey] = fromHeader;
                return fromHeader;
            }

            var generated = Activity.Current?.Id
                ?? httpContext.TraceIdentifier
                ?? Guid.NewGuid().ToString("N");

            // Keep user-facing IDs short when TraceIdentifier is long
            if (generated.Length > 24)
                generated = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

            httpContext.Items[ItemKey] = generated;
            return generated;
        }
    }

    public sealed class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = CorrelationId.Get(context);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationId.HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RequestPath"] = context.Request.Path.Value ?? string.Empty,
                ["RequestMethod"] = context.Request.Method
            }))
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// Catches unhandled exceptions, logs them fully with correlation ID,
    /// and returns a JSON or MVC error response (dev shows details).
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = CorrelationId.Get(context);
                var userId = context.User?.Identity?.Name ?? "anonymous";

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["UserId"] = userId
                }))
                {
                    _logger.LogError(
                        ex,
                        "Unhandled exception for {UserId} on {Method} {Path} at {UtcNow}. CorrelationId={CorrelationId}",
                        userId,
                        context.Request.Method,
                        context.Request.Path.Value,
                        DateTime.UtcNow,
                        correlationId);

                    _logger.LogError(
                        "Exception dump CorrelationId={CorrelationId} Type={ExceptionType} Message={ErrorMessage} Inner={InnerException} Stack={StackTrace}",
                        correlationId,
                        ex.GetType().FullName,
                        ex.Message,
                        ex.InnerException?.ToString() ?? "(none)",
                        ex.StackTrace ?? "(none)");
                }

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var acceptsJson = context.Request.Headers.Accept.Any(a =>
                    a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
                    || context.Request.Path.StartsWithSegments("/api");

                if (acceptsJson || string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = "application/json";
                    var payload = new
                    {
                        ok = false,
                        correlationId,
                        error = _env.IsDevelopment()
                            ? ex.GetBaseException().Message
                            : $"Something went wrong. Reference ID: {correlationId}",
                        detailedError = _env.IsDevelopment() ? ExceptionFormatter.Format(ex) : null
                    };
                    await context.Response.WriteAsJsonAsync(payload);
                    return;
                }

                context.Items["UnhandledException"] = ex;
                context.Items["UnhandledCorrelationId"] = correlationId;
                context.Response.Redirect($"/Home/Error?cid={Uri.EscapeDataString(correlationId)}");
            }
        }
    }

    public static class ExceptionFormatter
    {
        public static string Format(Exception ex)
        {
            var sb = new StringBuilder();
            var current = ex;
            var depth = 0;
            while (current != null && depth < 8)
            {
                if (depth > 0) sb.AppendLine().AppendLine("--- Inner exception ---");
                sb.AppendLine($"{current.GetType().FullName}: {current.Message}");
                if (!string.IsNullOrWhiteSpace(current.StackTrace))
                    sb.AppendLine(current.StackTrace);
                current = current.InnerException;
                depth++;
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Shared controller helpers: structured log + correlation ID + TempData/ModelState/JSON user messages.
    /// </summary>
    public static class ControllerError
    {
        public static string Capture(
            ControllerBase controller,
            ILogger logger,
            Exception ex,
            string messageTemplate,
            params object[] args)
        {
            var http = controller.HttpContext;
            var correlationId = CorrelationId.Get(http);
            var env = http.RequestServices.GetRequiredService<IHostEnvironment>();
            var userId = http.User?.Identity?.Name ?? "anonymous";

            var scopeState = new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["UserId"] = userId,
                ["RequestPath"] = http.Request.Path.Value ?? string.Empty
            };

            using (logger.BeginScope(scopeState))
            {
                // Caller template first (structured args), then always stamp correlation + time.
                logger.LogError(ex, messageTemplate, args);
                logger.LogError(
                    ex,
                    "Failed operation for {UserId} at {UtcNow}. CorrelationId={CorrelationId}",
                    userId,
                    DateTime.UtcNow,
                    correlationId);
                logger.LogError(
                    "Exception dump CorrelationId={CorrelationId} Type={ExceptionType} Message={ErrorMessage} Inner={InnerException} Stack={StackTrace}",
                    correlationId,
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.ToString() ?? "(none)",
                    ex.StackTrace ?? "(none)");
            }

            var userMessage = env.IsDevelopment()
                ? $"[{correlationId}] {ex.GetBaseException().Message}"
                : $"Something went wrong. Reference ID: {correlationId}";

            if (controller is Controller mvc)
            {
                mvc.TempData["Error"] = userMessage;
                mvc.TempData["CorrelationId"] = correlationId;
                if (env.IsDevelopment())
                    mvc.TempData["DetailedError"] = ExceptionFormatter.Format(ex);
                else
                    mvc.TempData.Remove("DetailedError");
            }

            return correlationId;
        }

        public static void AddModelError(
            Controller controller,
            ILogger logger,
            Exception ex,
            string messageTemplate,
            params object[] args)
        {
            var correlationId = Capture(controller, logger, ex, messageTemplate, args);
            var env = controller.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var message = env.IsDevelopment()
                ? $"[{correlationId}] {ex.GetBaseException().Message}"
                : $"Something went wrong. Reference ID: {correlationId}";
            controller.ModelState.AddModelError(string.Empty, message);
        }

        public static object JsonError(
            ControllerBase controller,
            ILogger logger,
            Exception ex,
            string messageTemplate,
            params object[] args)
        {
            var correlationId = Capture(controller, logger, ex, messageTemplate, args);
            var env = controller.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            return new
            {
                ok = false,
                correlationId,
                error = env.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : $"Something went wrong. Reference ID: {correlationId}",
                detailedError = env.IsDevelopment() ? ExceptionFormatter.Format(ex) : null
            };
        }

        /// <summary>
        /// ERP-style required-field toast used across modules.
        /// </summary>
        public static void RequiredDetailWarning(Controller controller)
        {
            controller.TempData["Warning"] = "Please Fill Required Detail.";
        }

        /// <summary>
        /// For non-fatal optional loads: still log the full exception at Warning with correlation ID.
        /// </summary>
        public static string CaptureWarning(
            ControllerBase controller,
            ILogger logger,
            Exception ex,
            string messageTemplate,
            params object[] args)
        {
            var correlationId = CorrelationId.Get(controller.HttpContext);
            var userId = controller.HttpContext.User?.Identity?.Name ?? "anonymous";

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["UserId"] = userId
            }))
            {
                logger.LogWarning(ex, messageTemplate, args);
                logger.LogWarning(
                    ex,
                    "Non-fatal failure for {UserId} at {UtcNow}. CorrelationId={CorrelationId}",
                    userId,
                    DateTime.UtcNow,
                    correlationId);
                logger.LogWarning(
                    "Exception dump CorrelationId={CorrelationId} Type={ExceptionType} Message={ErrorMessage} Inner={InnerException} Stack={StackTrace}",
                    correlationId,
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.ToString() ?? "(none)",
                    ex.StackTrace ?? "(none)");
            }

            return correlationId;
        }
    }
}
