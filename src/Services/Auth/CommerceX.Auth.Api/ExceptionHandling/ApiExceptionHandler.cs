using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CommerceX.Auth.Api.ExceptionHandling;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiExceptionHandler(
        IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        string title;
        string detail;
        string type;

        switch (exception)
        {
            case ArgumentNullException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Bad Request";
                detail = "The request is invalid.";
                type = "https://commercex/errors/bad-request";
                break;

            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation failed";
                detail = exception.Message;
                type = "https://commercex/errors/validation";
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                detail = exception.Message;
                type = "https://commercex/errors/unauthorized";
                break;

            case InvalidOperationException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = exception.Message;
                type = "https://commercex/errors/conflict";
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Internal Server Error";
                detail = "An unexpected error occurred.";
                type = "https://commercex/errors/internal-server-error";
                break;
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });
    }
}