namespace CleanWebApi.Application.Common.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// The spans and metrics the Application layer publishes. Both use the name in <see cref="Name"/>, which the
/// presentation layer's startup subscribes to. Nothing is recorded unless something is listening, so the layer stays
/// free of any telemetry package.
/// </summary>
public sealed class ApplicationTelemetry
{
    /// <summary>The name of the activity source and the meter.</summary>
    public const string Name = "CleanWebApi.Application";

    /// <summary>Starts a span for each use case.</summary>
    internal static readonly ActivitySource ActivitySource = new(Name);

    private readonly Counter<long> requests;
    private readonly Histogram<double> duration;

    /// <summary>Creates the meter and its instruments.</summary>
    /// <param name="meterFactory">Owns the meter, so it's disposed with the container.</param>
    public ApplicationTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);

        this.requests = meter.CreateCounter<long>(
            "application.requests",
            "{request}",
            "The use case requests sent through the pipeline.");

        this.duration = meter.CreateHistogram(
            "application.request.duration",
            "s",
            "How long each use case request took.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10] });
    }

    /// <summary>Records one finished request on both instruments.</summary>
    /// <param name="requestName">The request's type name.</param>
    /// <param name="outcome">How it ended, one of the <see cref="RequestOutcome"/> values.</param>
    /// <param name="elapsed">How long it took.</param>
    public void RecordRequest(string requestName, string outcome, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { TagNames.RequestName, requestName },
            { TagNames.RequestOutcome, outcome }
        };

        this.requests.Add(1, tags);
        this.duration.Record(elapsed.TotalSeconds, tags);
    }

    /// <summary>The tag names used on the spans and metrics.</summary>
    public static class TagNames
    {
        /// <summary>The request's type name, such as <c>GetMoviesQuery</c>.</summary>
        public const string RequestName = "application.request.name";

        /// <summary>How the request ended.</summary>
        public const string RequestOutcome = "application.request.outcome";

        /// <summary>The properties that failed validation, comma separated.</summary>
        public const string ValidationProperties = "application.validation.properties";
    }

    /// <summary>How a request ended.</summary>
    public static class RequestOutcome
    {
        /// <summary>The handler returned a response.</summary>
        public const string Handled = "handled";

        /// <summary>The request failed validation.</summary>
        public const string Invalid = "invalid";

        /// <summary>What the request asked for doesn't exist.</summary>
        public const string NotFound = "not_found";

        /// <summary>The caller cancelled the request.</summary>
        public const string Cancelled = "cancelled";

        /// <summary>Something unexpected went wrong.</summary>
        public const string Error = "error";
    }
}
