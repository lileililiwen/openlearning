using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Logging.Services;

namespace OpenLearning.Logging.Middleware;

/// <summary>
/// Persists unhandled exceptions into <c>ErrorLog</c> with request context,
/// then rethrows so the configured exception handler still renders the error
/// page. Logging is best-effort and never masks the original exception.
/// </summary>
public sealed class LoggingExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var logs = scope.ServiceProvider.GetRequiredService<LogService>();
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                await logs.LogErrorAsync(
                    ex.Message,
                    ex.StackTrace,
                    context.Request.Path,
                    context.Request.Method,
                    userId);
            }
            catch
            {
                // Best effort: never replace the original exception.
            }

            throw;
        }
    }
}
