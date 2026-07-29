using System.Net;
using System.Text.Json;
using FluentValidation;
using ProductApi.Application.DTOs;
using ProductApi.Domain.Exceptions;

namespace ProductApi.API.Middleware;

/// <summary>
/// Central error-handling middleware. Converts every exception into the
/// consistent ApiErrorResponse envelope and the correct HTTP status code,
/// per the "Error Handling Middleware" / "consistent error response format" requirements.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ApiErrorResponse { TraceId = context.TraceIdentifier };

        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            ValidationAppException => HttpStatusCode.BadRequest,
            FluentValidation.ValidationException => HttpStatusCode.BadRequest,
            UnauthorizedAppException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        response.StatusCode = (int)statusCode;
        response.Message = exception.Message;

        if (exception is ValidationAppException validationAppException)
        {
            response.Errors = validationAppException.Errors;
        }
        else if (exception is FluentValidation.ValidationException fluentEx)
        {
            response.Errors = fluentEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            response.Message = "An unexpected error occurred. Please try again later.";
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}", response.StatusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
