using Microsoft.AspNetCore;

namespace Blue.Web;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(o =>
                {
                    o.AddJsonFile("appsettings.json")
                        .AddJsonFile("appsettings.local.json", optional: true);
                })
                .ConfigureLogging(o => o.AddConsole())
                .UseStartup<Startup>()
                .Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }
}