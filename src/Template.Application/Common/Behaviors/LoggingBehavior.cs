using MediatR;
using Microsoft.Extensions.Logging;

namespace Template.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling request {RequestName} with payload {@Request}", requestName, request);

        var response = await next();

        logger.LogInformation("Completed request {RequestName} with response {@Response}", requestName, response);
        return response;
    }
}
