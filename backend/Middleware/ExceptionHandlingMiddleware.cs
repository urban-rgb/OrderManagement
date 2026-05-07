using Microsoft.EntityFrameworkCore;
using backend.Domain.Exceptions;

namespace backend.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = exception switch
        {
            KeyNotFoundDomainException => StatusCodes.Status404NotFound,
            ConflictDomainException or DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = code;

        var message = exception.Message;
        if (exception.InnerException != null)
        {
            message += " -> " + exception.InnerException.Message;
        }

        return context.Response.WriteAsJsonAsync(new { error = message });
    }
}