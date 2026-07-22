using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MatterHarbor.Api.Authentication;
using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Api.ErrorHandling;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "validation-error"),
            AssignedUserNotFoundException => (StatusCodes.Status400BadRequest, "Validation failed", "validation-error"),
            InvalidUserContextException => (StatusCodes.Status403Forbidden, "Forbidden", "invalid-user-context"),
            CaseNotFoundException => (StatusCodes.Status404NotFound, "Case not found", "case-not-found"),
            IdempotencyConflictException => (StatusCodes.Status409Conflict, "Idempotency conflict", "idempotency-conflict"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict", "concurrency-conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "unexpected-error")
        };

        if (status >= 500)
        {
            LogUnhandledError(logger, exception.GetType().Name);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://matterharbor.dev/problems/{type}",
                Detail = status < 500 ? exception.Message : null
            },
            Exception = exception
        });
    }

    [LoggerMessage(2001, LogLevel.Error, "Unhandled API error {ErrorCode}")]
    private static partial void LogUnhandledError(ILogger logger, string errorCode);
}
