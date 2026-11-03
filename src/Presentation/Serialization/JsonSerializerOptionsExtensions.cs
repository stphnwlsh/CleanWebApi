namespace CleanWebApi.Presentation.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// The JSON settings for every request and response, kept in one place so the API and its tests read and write the same shapes.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Applies the API's JSON settings: camelCase property names matched without case on the way in, nulls left out,
    /// numbers only as JSON numbers and enums as camelCase strings.
    /// </summary>
    /// <param name="options">The serializer options to change.</param>
    /// <returns>The same options, so calls can be chained.</returns>
    public static JsonSerializerOptions UseApiDefaults(this JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
