using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Currency.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http,
        Exception ex,
        CancellationToken ct
    )
    {
        var (status, title, type, detail) = ex switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "https://httpstatuses.com/400",
                (string?)null
            ),
            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "Upstream service error",
                "https://httpstatuses.com/502",
                ex.Message
            ),
            TimeoutException or TaskCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Upstream timeout",
                "https://httpstatuses.com/504",
                "The upstream request timed out."
            ),
            InvalidOperationException => (
                StatusCodes.Status422UnprocessableEntity,
                "Cannot process request",
                "https://httpstatuses.com/422",
                ex.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "https://httpstatuses.com/500",
                "An unexpected error occurred."
            ),
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = http.Request.Path,
        };

        if (ex is ValidationException ve)
        {
            var errors = ve
                .Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            problem.Extensions["errors"] = errors;
        }

        http.Response.StatusCode = status;
        http.Response.ContentType = "application/problem+json";
        await http.Response.WriteAsJsonAsync(problem, cancellationToken: ct);
        return true;
    }
}
