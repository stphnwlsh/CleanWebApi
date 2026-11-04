namespace CleanWebApi.Infrastructure.Telemetry;

using System.Diagnostics;

/// <summary>
/// The spans and meters Infrastructure publishes, for the presentation layer's startup to subscribe to.
/// </summary>
public static class InfrastructureTelemetry
{
    /// <summary>The name of the activity source that gives each database command a span.</summary>
    public const string Name = "CleanWebApi.Infrastructure";

    /// <summary>The meter EF Core publishes itself: queries, saves, active contexts and the compiled query cache.</summary>
    public const string EntityFrameworkCoreMeterName = "Microsoft.EntityFrameworkCore";

    /// <summary>Starts a span for each database command.</summary>
    internal static readonly ActivitySource ActivitySource = new(Name);
}
