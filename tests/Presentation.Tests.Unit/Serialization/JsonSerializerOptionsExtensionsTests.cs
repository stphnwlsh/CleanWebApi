namespace CleanWebApi.Presentation.Tests.Unit.Serialization;

using System.Text.Json;
using Presentation.Serialization;
using Shouldly;
using Xunit;

public class JsonSerializerOptionsExtensionsTests
{
    [Fact]
    public void UseApiDefaults_ShouldWrite_CamelCaseWithoutNulls()
    {
        // Arrange
        var options = new JsonSerializerOptions().UseApiDefaults();

        // Act
        var result = JsonSerializer.Serialize(new Sample("Value", null, Colour.DarkRed), options);

        // Assert
        result.ShouldBe(/*lang=json,strict*/ """{"name":"Value","colour":"darkRed"}""");
    }

    [Fact]
    public void UseApiDefaults_ShouldRead_PropertyNamesInAnyCase()
    {
        // Arrange
        var options = new JsonSerializerOptions().UseApiDefaults();

        // Act
        var result = JsonSerializer.Deserialize<Sample>(/*lang=json,strict*/ """{"NAME":"Value","Colour":"darkRed"}""", options);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Name.ShouldBe("Value");
        result.Colour.ShouldBe(Colour.DarkRed);
    }

    [Fact]
    public void UseApiDefaults_ShouldReject_NumbersSentAsStrings()
    {
        // Arrange
        var options = new JsonSerializerOptions().UseApiDefaults();

        // Act
        var exception = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Rating>(/*lang=json,strict*/ """{"stars":"5"}""", options));

        // Assert
        exception.Path.ShouldBe("$.stars");
    }

    [Fact]
    public void UseApiDefaults_ShouldThrow_OptionsAreNull()
    {
        // Arrange
        JsonSerializerOptions options = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(options.UseApiDefaults);

        // Assert
        exception.ParamName.ShouldBe("options");
    }

    public enum Colour
    {
        DarkRed
    }

    public sealed record Sample(string Name, string? Nickname, Colour Colour);

    public sealed record Rating(int Stars);
}
