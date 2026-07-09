using Haven.Application.Common.Exceptions;
using Haven.Application.Common.Responses;
using Haven.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace Haven.Presentation.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;
        int statusCode;

        if (exception is ValidationException validationEx && validationEx.Errors.Count > 0)
        {
            statusCode = StatusCodes.Status422UnprocessableEntity;
            response = new ValidationErrorResponse(false, "Validation failed", validationEx.Errors);
        }
        else
        {
            (statusCode, response) = exception switch
            {
                NotFoundException => (
                    StatusCodes.Status404NotFound,
                    new ApiResponse(false, exception.Message)
                ),
                ValidationException => (
                    StatusCodes.Status400BadRequest,
                    new ApiResponse(false, exception.Message)
                ),
                ForbiddenException => (
                    StatusCodes.Status403Forbidden,
                    new ApiResponse(false, exception.Message)
                ),
                GitHubOAuthNotConfiguredException => (
                    StatusCodes.Status400BadRequest,
                    new ApiResponse(false, exception.Message)
                ),
                HavenException => (
                    StatusCodes.Status500InternalServerError,
                    new ApiResponse(false, exception.Message)
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    new ApiResponse(false, "An unexpected error occurred. Please try again later.")
                )
            };
        }

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}