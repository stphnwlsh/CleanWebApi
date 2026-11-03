namespace CleanWebApi.Application.Tests.Unit.Version.Queries.GetVersion;

using System.Reflection;
using Application.Versions.Queries.GetVersion;
using Shouldly;
using Xunit;

public class GetVersionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnVersions_AssemblyHasVersionAttributes()
    {
        // Arrange
        var assembly = typeof(GetVersionHandlerTests).Assembly;
        var handler = new GetVersionHandler(assembly);
        using var tokenSource = new CancellationTokenSource();

        // Act
        var result = await handler.Handle(new GetVersionQuery(), tokenSource.Token);

        // Assert
        result.FileVersion.ShouldBe(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);
        result.InformationalVersion.ShouldBe(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyVersions_NoAssembly()
    {
        // Arrange
        var handler = new GetVersionHandler(null);
        using var tokenSource = new CancellationTokenSource();

        // Act
        var result = await handler.Handle(new GetVersionQuery(), tokenSource.Token);

        // Assert
        result.FileVersion.ShouldBeEmpty();
        result.InformationalVersion.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReadEntryAssembly_DefaultConstructor()
    {
        // Arrange
        var handler = new GetVersionHandler();
        using var tokenSource = new CancellationTokenSource();

        // Act
        var result = await handler.Handle(new GetVersionQuery(), tokenSource.Token);

        // Assert
        result.FileVersion.ShouldNotBeNullOrWhiteSpace();
    }
}
