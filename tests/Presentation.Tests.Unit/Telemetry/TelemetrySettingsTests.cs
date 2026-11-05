namespace CleanWebApi.Presentation.Tests.Unit.Telemetry;

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration;
using Presentation.Telemetry;
using Shouldly;
using Xunit;

public class TelemetrySettingsTests
{
    [Fact]
    public void FromConfiguration_ShouldReturn_DisabledDefaults_WhenNothingIsSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var assembly = typeof(TelemetrySettingsTests).Assembly;

        // Act
        var result = TelemetrySettings.FromConfiguration(configuration, "local", assembly);

        // Assert
        result.Enabled.ShouldBeFalse();
        result.ServiceName.ShouldBe(TelemetrySettings.DefaultServiceName);
        result.ServiceVersion.ShouldBe(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
        result.EnvironmentName.ShouldBe("local");
    }

    [Fact]
    public void FromConfiguration_ShouldReturn_EnabledSettings_WhenEndpointIsSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://aspire-dashboard:18889",
                ["OTEL_SERVICE_NAME"] = "movie-reviews"
            })
            .Build();

        // Act
        var result = TelemetrySettings.FromConfiguration(configuration, "docker", typeof(TelemetrySettingsTests).Assembly);

        // Assert
        result.Enabled.ShouldBeTrue();
        result.OtlpEndpoint.ShouldBe("http://aspire-dashboard:18889");
        result.ResourceAttributes["service.name"].ShouldBe("movie-reviews");
        result.ResourceAttributes["service.version"].ShouldBe(result.ServiceVersion);
        result.ResourceAttributes["deployment.environment.name"].ShouldBe("docker");
    }

    [Fact]
    public void FromConfiguration_ShouldReturn_UnknownVersion_WhenAssemblyHasNone()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NoVersion"), AssemblyBuilderAccess.Run);

        // Act
        var result = TelemetrySettings.FromConfiguration(configuration, "local", assembly);

        // Assert
        result.ServiceVersion.ShouldBe("unknown");
    }
}
