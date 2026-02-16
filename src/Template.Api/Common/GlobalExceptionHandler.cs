using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Template.Api.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            var problemDetails = new HttpValidationProblemDetails(errors)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest
            };
            problemDetails.Extensions["errorCode"] = "validation.failed";

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await Results.Problem(problemDetails).ExecuteAsync(httpContext);
            return true;
        }

        logger.LogError(exception, "Unhandled exception encountered while processing request");

        var serverProblem = new ProblemDetails
        {
            Title = "Server Error",
            Detail = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError
        };
        serverProblem.Extensions["errorCode"] = "server.unexpected";

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(serverProblem).ExecuteAsync(httpContext);

        return true;
    }
}
