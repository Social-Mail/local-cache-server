using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace LocalCache;

internal class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args); // or HostApplicationBuilder.Create(args); in newer versions

        builder.ConfigureServices((hostContext, services) =>
        {
            services.AddMemoryCache();

            services.AddSingleton<BucketStore>();

            // Register your services here
            services.AddHostedService<LocalCacheServer>();

        });

        var host = builder.Build();

        // Run your application logic
        host.Run();
    }

}
