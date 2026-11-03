namespace CleanWebApi.Application.Common.Behaviours;

using Exceptions;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs how every request ended and how long it took. Validation failures and missing entities are logged as
/// information, because the caller gets an answer. Anything else is left to the presentation layer, which turns it
/// into a response and logs it once as an error.
/// </summary>
/// <typeparam name="TMessage">The request being sent.</typeparam>
/// <typeparam name="TResponse">The response the handler returns.</typeparam>
/// <param name="logger">Where the log events go.</param>
/// <param name="timeProvider">The clock that times each request.</param>
public partial class LoggingBehaviour<TMessage, TResponse>(ILogger<LoggingBehaviour<TMessage, TResponse>> logger, TimeProvider timeProvider) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly string RequestName = typeof(TMessage).Name;

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();

        try
        {
            var response = await next(message, cancellationToken);
            var elapsed = timeProvider.GetElapsedTime(started).TotalMilliseconds;

            LogHandled(logger, RequestName, elapsed);

            return response;
        }
        catch (ValidationException exception)
        {
            var elapsed = timeProvider.GetElapsedTime(started).TotalMilliseconds;
            var properties = string.Join(", ", exception.Errors.Select(e => e.PropertyName).Distinct());

            LogInvalid(logger, RequestName, elapsed, properties);

            throw;
        }
        catch (NotFoundException exception)
        {
            var elapsed = timeProvider.GetElapsedTime(started).TotalMilliseconds;

            LogNotFound(logger, RequestName, elapsed, exception.Message);

            throw;
        }
    }

    #region Log Messages

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} handled in {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogHandled(ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} failed validation in {ElapsedMilliseconds:0.0} ms for {PropertyNames}")]
    private static partial void LogInvalid(ILogger logger, string requestName, double elapsedMilliseconds, string propertyNames);

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} found nothing in {ElapsedMilliseconds:0.0} ms: {Reason}")]
    private static partial void LogNotFound(ILogger logger, string requestName, double elapsedMilliseconds, string reason);

    #endregion Log Messages
}
