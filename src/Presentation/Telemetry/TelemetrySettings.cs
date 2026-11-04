namespace CleanWebApi.Presentation.Telemetry;

using System.Reflection;
using Microsoft.Extensions.Configuration;

/// <summary>
/// What every span, metric and log carries, and where they go. Telemetry is always collected, but it's only exported
/// when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set. Every other <c>OTEL_*</c> setting the OpenTelemetry SDK knows, such as
/// <c>OTEL_EXPORTER_OTLP_PROTOCOL</c> or <c>OTEL_EXPORTER_OTLP_HEADERS</c>, works as usual.
/// </summary>
public class TelemetrySettings
{
    /// <summary>The service name used when <c>OTEL_SERVICE_NAME</c> isn't set.</summary>
    public const string DefaultServiceName = "CleanWebApi";

    /// <summary>The <c>service.name</c> on everything this app sends.</summary>
    public required string ServiceName { get; init; }

    /// <summary>The <c>service.version</c>: the informational version of the presentation assembly.</summary>
    public required string ServiceVersion { get; init; }

    /// <summary>The <c>deployment.environment.name</c>, such as <c>local</c> or <c>docker</c>.</summary>
    public required string EnvironmentName { get; init; }

    /// <summary>Where the OTLP exporters send everything. Empty means nothing is exported.</summary>
    public string? OtlpEndpoint { get; init; }

    public bool Enabled => !string.IsNullOrWhiteSpace(this.OtlpEndpoint);

    /// <summary>The resource attributes, so traces, metrics and logs all land on the same service.</summary>
    public Dictionary<string, object> ResourceAttributes => new()
    {
        ["service.name"] = this.ServiceName,
        ["service.version"] = this.ServiceVersion,
        ["deployment.environment.name"] = this.EnvironmentName
    };

    public static TelemetrySettings FromConfiguration(IConfiguration configuration, string environmentName, Assembly assembly)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"];

        return new TelemetrySettings
        {
            ServiceName = string.IsNullOrWhiteSpace(serviceName) ? DefaultServiceName : serviceName,
            ServiceVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            EnvironmentName = environmentName,
            OtlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        };
    }
}
