
using System.Net;
using System.Text.Json;
using Predixus.Application.Exceptions;

namespace Predixus.API.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "İşlenmeyen hata: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException ex         => (HttpStatusCode.NotFound, ex.Message),
            ConflictException ex         => (HttpStatusCode.Conflict, ex.Message),
            UnauthorizedException ex     => (HttpStatusCode.Unauthorized, ex.Message),
            InsufficientDataException ex => (HttpStatusCode.BadRequest, ex.Message),
            ExternalServiceException ex  => (HttpStatusCode.ServiceUnavailable, ex.Message),
            _                            => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        var response = new
        {
            status = (int)statusCode,
            message
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
