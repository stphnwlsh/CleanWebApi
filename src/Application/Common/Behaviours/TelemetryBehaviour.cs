namespace CleanWebApi.Application.Common.Behaviours;

using System.Diagnostics;
using Exceptions;
using FluentValidation;
using Mediator;
using Telemetry;

/// <summary>
/// Gives every request a span named after it and records it on the request counter and duration histogram. Baggage
/// from the caller is copied onto the span as tags. Validation failures and missing entities become span events,
/// because the caller still gets an answer, and anything else marks the span as an error with the exception attached.
/// </summary>
/// <typeparam name="TMessage">The request being sent.</typeparam>
/// <typeparam name="TResponse">The response the handler returns.</typeparam>
/// <param name="telemetry">Records the request metrics.</param>
/// <param name="timeProvider">The clock that times each request.</param>
public class TelemetryBehaviour<TMessage, TResponse>(ApplicationTelemetry telemetry, TimeProvider timeProvider) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly string RequestName = typeof(TMessage).Name;

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ApplicationTelemetry.ActivitySource.StartActivity(RequestName);

        if (activity is { IsAllDataRequested: true })
        {
            _ = activity.SetTag(ApplicationTelemetry.TagNames.RequestName, RequestName);

            foreach (var (key, value) in activity.Baggage)
            {
                _ = activity.SetTag(key, value);
            }
        }

        var started = timeProvider.GetTimestamp();
        var outcome = ApplicationTelemetry.RequestOutcome.Handled;

        try
        {
            return await next(message, cancellationToken);
        }
        catch (ValidationException exception)
        {
            outcome = ApplicationTelemetry.RequestOutcome.Invalid;

            _ = activity?.AddEvent(new ActivityEvent("validation_failed", tags: new ActivityTagsCollection
            {
                [ApplicationTelemetry.TagNames.ValidationProperties] = string.Join(", ", exception.Errors.Select(e => e.PropertyName).Distinct())
            }));

            throw;
        }
        catch (NotFoundException exception)
        {
            outcome = ApplicationTelemetry.RequestOutcome.NotFound;

            _ = activity?.AddEvent(new ActivityEvent("not_found", tags: new ActivityTagsCollection
            {
                ["exception.message"] = exception.Message
            }));

            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = ApplicationTelemetry.RequestOutcome.Cancelled;

            throw;
        }
        catch (Exception exception)
        {
            outcome = ApplicationTelemetry.RequestOutcome.Error;

            _ = activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            _ = activity?.AddException(exception);

            throw;
        }
        finally
        {
            _ = activity?.SetTag(ApplicationTelemetry.TagNames.RequestOutcome, outcome);

            telemetry.RecordRequest(RequestName, outcome, timeProvider.GetElapsedTime(started));
        }
    }
}
