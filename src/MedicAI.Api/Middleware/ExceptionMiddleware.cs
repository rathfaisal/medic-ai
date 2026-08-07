using System.Net;
using System.Text.Json;

namespace MedicAI.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, exception);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        var message = statusCode == HttpStatusCode.InternalServerError
            ? "An unexpected server error occurred."
            : exception.Message;

        var body = JsonSerializer.Serialize(new
        {
            error = message,
            status = (int)statusCode,
            traceId = context.TraceIdentifier
        });
        await context.Response.WriteAsync(body);
    }
}
