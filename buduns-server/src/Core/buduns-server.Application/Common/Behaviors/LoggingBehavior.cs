using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace buduns_server.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var responseName = typeof(TResponse).Name;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next(cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "CQRS request completed. RequestName: {RequestName}, ResponseName: {ResponseName}, ElapsedMilliseconds: {ElapsedMilliseconds}",
                    requestName,
                    responseName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "CQRS request failed. RequestName: {RequestName}, ResponseName: {ResponseName}, ElapsedMilliseconds: {ElapsedMilliseconds}, ExceptionType: {ExceptionType}",
                    requestName,
                    responseName,
                    stopwatch.ElapsedMilliseconds,
                    exception.GetType().Name);

                throw;
            }
        }
    }
}
