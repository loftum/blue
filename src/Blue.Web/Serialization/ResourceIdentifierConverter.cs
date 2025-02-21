using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;

namespace Blue.Web.Serialization;

public class ResourceIdentifierConverter : JsonConverter<ResourceIdentifier>
{
    public override ResourceIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (s == null)
        {
            return null;
        }

        return new ResourceIdentifier(s);
    }

    public override void Write(Utf8JsonWriter writer, ResourceIdentifier value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}