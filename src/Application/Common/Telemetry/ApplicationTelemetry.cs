namespace CleanWebApi.Application.Common.Telemetry;

using System.Diagnostics;

/// <summary>
/// The spans the Application layer publishes, one for each use case. The presentation layer's startup subscribes to
/// <see cref="Name"/>. Nothing is recorded unless something is listening, so the layer needs no telemetry package.
/// </summary>
public static class ApplicationTelemetry
{
    /// <summary>The name of the activity source.</summary>
    public const string Name = "CleanWebApi.Application";

    /// <summary>The tag holding the request's type name, such as <c>GetMoviesQuery</c>.</summary>
    public const string RequestNameTag = "application.request.name";

    /// <summary>The tag holding how the request ended: handled, invalid, not_found, cancelled or error.</summary>
    public const string RequestOutcomeTag = "application.request.outcome";

    /// <summary>Starts a span for each use case.</summary>
    internal static readonly ActivitySource ActivitySource = new(Name);
}
