using System.Text.Json;

namespace LibraryAPI.Middleware;

/// <summary>
/// ASP.NET Core middleware that catches any unhandled exception in the request pipeline
/// and returns a safe JSON error response.
/// Layer: Middleware
///
/// Why do we need this?
/// Without it, an unhandled exception would either return an empty 500 response or,
/// in development mode, return a stack trace page — neither is acceptable for an API.
/// This middleware ensures every error that escapes the controller catch blocks is still
/// returned as { "error": "..." } with status 500, and that no internal details
/// (stack traces, file paths, SQL errors) are leaked to the caller.
///
/// Registration order matters: this must be registered BEFORE app.MapControllers() in
/// Program.cs so it wraps the entire controller pipeline.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Called for every HTTP request. Passes the request to the next middleware/controller
    /// in the pipeline. If that throws an unhandled exception, catches it, logs it
    /// server-side, and returns a generic 500 JSON response to the client.
    ///
    /// The log preserves the real exception (with stack trace) for internal debugging
    /// while the response body contains only a safe, generic message.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the full exception server-side so developers can debug it.
            // The client never sees this.
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = new { error = "An unexpected error occurred." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
