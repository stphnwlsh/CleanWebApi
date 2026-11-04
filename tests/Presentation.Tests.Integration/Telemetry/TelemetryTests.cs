namespace CleanWebApi.Presentation.Tests.Integration.Telemetry;

using System.Diagnostics;
using System.Net;
using Application.Common.Telemetry;
using Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

public sealed class TelemetryTests : IDisposable
{
    private const string AspNetCoreSourceName = "Microsoft.AspNetCore";

    private readonly ActivityListener listener;
    private readonly List<Activity> spans = [];

    public TelemetryTests()
    {
        // Other tests send requests at the same time, so only spans from this test's own trace are kept.
        this.listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is AspNetCoreSourceName or ApplicationTelemetry.Name or InfrastructureTelemetry.Name,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (this.spans)
                {
                    this.spans.Add(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(this.listener);
    }

    [Fact]
    public async Task Request_ShouldTrace_FromHttpThroughUseCaseToDatabase()
    {
        // Arrange
        using var application = new WebApiApplication();
        using var client = application.CreateClient();

        var traceId = ActivityTraceId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/movies?limit=5");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");
        request.Headers.Add("baggage", "tenant.id=acme");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var trace = this.SpansIn(traceId);
        var server = trace.Single(s => s.Source.Name == AspNetCoreSourceName);
        var useCase = trace.Single(s => s.Source.Name == ApplicationTelemetry.Name);
        var commands = trace.Where(s => s.Source.Name == InfrastructureTelemetry.Name).ToList();

        server.Kind.ShouldBe(ActivityKind.Server);
        useCase.DisplayName.ShouldBe("GetMoviesQuery");
        useCase.ParentSpanId.ShouldBe(server.SpanId);
        useCase.GetTagItem("tenant.id").ShouldBe("acme");
        commands.ShouldNotBeEmpty();
        commands.ShouldAllBe(c => c.ParentSpanId == useCase.SpanId);
    }

    [Fact]
    public async Task HealthRequest_ShouldNotBeTraced()
    {
        // Arrange
        using var application = new WebApiApplication();
        using var client = application.CreateClient();

        var traceId = ActivityTraceId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        this.SpansIn(traceId).ShouldNotContain(s => s.Source.Name == AspNetCoreSourceName && s.IsAllDataRequested && s.Recorded);
    }

    [Fact]
    public async Task Startup_ShouldRegister_TracerAndMeterProviders_WhenExporting()
    {
        // Arrange
        using var application = new WebApiApplication(settings: new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
            ["OTEL_SERVICE_NAME"] = "TelemetryTests"
        });
        using var client = application.CreateClient();

        // Act
        using var response = await client.GetAsync("/health/live");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = application.Services.GetRequiredService<TracerProvider>();
        _ = application.Services.GetRequiredService<MeterProvider>();
    }

    public void Dispose()
    {
        this.listener.Dispose();
    }

    private List<Activity> SpansIn(ActivityTraceId traceId)
    {
        lock (this.spans)
        {
            return [.. this.spans.Where(s => s.TraceId == traceId)];
        }
    }
}
