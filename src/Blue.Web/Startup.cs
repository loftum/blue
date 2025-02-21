using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Blue.Core;
using Blue.Web.Serialization;

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