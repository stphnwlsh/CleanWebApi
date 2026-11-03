namespace CleanWebApi.Presentation.Tests.Integration.Extensions;

using System.Text.Json;
using CleanWebApi.Presentation.Serialization;

public static class JsonSerializerExtensions
{
    // The same settings as the API, so a test reads a response exactly the way a client would.
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions().UseApiDefaults();

    public static string Serialize(this object input)
    {
        return JsonSerializer.Serialize(input, Options);
    }

    public static T Deserialize<T>(this string input)
    {
        return JsonSerializer.Deserialize<T>(input, Options)!;
    }
}
