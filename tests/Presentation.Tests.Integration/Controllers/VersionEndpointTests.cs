namespace CleanWebApi.Presentation.Tests.Integration.Controllers;

using System.Net;
using System.Threading.Tasks;
using Application.Versions.Entities;
using Extensions;
using Shouldly;
using Xunit;

public class VersionEndpointTests
{
    private static readonly WebApiApplication Application = new();

    [Fact]
    public async Task GetVersion_ShouldReturn_Ok()
    {
        // Arrange
        using var client = Application.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/version");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<Version>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();

        _ = result.FileVersion.ShouldBeOfType<string>();
        result.FileVersion.ShouldNotBeNullOrWhiteSpace();
        _ = result.InformationalVersion.ShouldBeOfType<string>();
        result.InformationalVersion.ShouldNotBeNullOrWhiteSpace();
    }
}
