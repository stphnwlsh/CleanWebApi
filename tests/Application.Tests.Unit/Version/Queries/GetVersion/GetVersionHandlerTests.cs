namespace CleanWebApi.Application.Tests.Unit.Version.Queries.GetVersion;

using System.Reflection;
using Application.Versions.Entities;
using Application.Versions.Queries.GetVersion;
using Shouldly;
using Xunit;

public class GetVersionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturn_VersionFromHost()
    {
        // Arrange
        var version = new VersionInfo { FileVersion = "1.2.0.0", InformationalVersion = "1.2.0+abc1234" };
        var handler = new GetVersionHandler(version);
        using var tokenSource = new CancellationTokenSource();

        // Act
        var result = await handler.Handle(new GetVersionQuery(), tokenSource.Token);

        // Assert
        result.ShouldBeSameAs(version);
    }

    [Fact]
    public void FromAssembly_ShouldReadVersions_AssemblyHasVersionAttributes()
    {
        // Arrange
        var assembly = typeof(GetVersionHandlerTests).Assembly;

        // Act
        var result = VersionInfo.FromAssembly(assembly);

        // Assert
        result.FileVersion.ShouldBe(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);
        result.InformationalVersion.ShouldBe(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
    }

    [Fact]
    public void FromAssembly_ShouldReturnEmptyVersions_NoAssembly()
    {
        // Act
        var result = VersionInfo.FromAssembly(null);

        // Assert
        result.FileVersion.ShouldBeEmpty();
        result.InformationalVersion.ShouldBeEmpty();
    }
}
