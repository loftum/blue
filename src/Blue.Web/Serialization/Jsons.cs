using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blue.Web.Serialization;

public static class Jsons
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ResourceIdentifierConverter());
        options.AllowTrailingCommas = true;
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    }
}