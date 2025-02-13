using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Blue.Core;

namespace Blue.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new AzureDeveloperCliCredential(),
            new AzurePowerShellCredential(),
            new InteractiveBrowserCredential());
        services.AddSingleton<TokenCredential>(credential);
        var mvc = services.AddMvc();
        mvc.AddJsonOptions(o => Jsons.Configure(o.JsonSerializerOptions));
        mvc.AddRazorRuntimeCompilation();

        var client = new ArmClient(credential);
        services.AddSingleton(client);
        services.AddSingleton<BlueClient>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseEndpoints(e => e.MapControllers());
    }
}

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