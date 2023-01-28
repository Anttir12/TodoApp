using System.Net;
using System.Text.Json;
using TodoApp.Exceptions;

namespace TodoApp;

public class CustomExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public CustomExceptionHandlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger<CustomExceptionHandlingMiddleware>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        string message = string.Empty;
        HttpStatusCode status;

        var exceptionType = exception.GetType();
        if (exceptionType == typeof(InvalidForeignKeyException))
        {
            message = exception.Message;
            status = HttpStatusCode.BadRequest;
        }
        else if (exceptionType == typeof(TooManyTasksException))
        {
            message = exception.Message;
            status = HttpStatusCode.BadRequest;
        }
        else
        {
            message = "Internal Server error. Contact support if this continues";
            status = HttpStatusCode.InternalServerError;
            _logger.LogError(exception.StackTrace);
        }
        var exceptionResult = JsonSerializer.Serialize(new { error = message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        await context.Response.WriteAsync(exceptionResult);
    }
}