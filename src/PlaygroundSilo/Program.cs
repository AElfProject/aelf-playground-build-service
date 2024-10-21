using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GrainInterfaces;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .UseConsoleLifetime();

// add services to builder
builder.ConfigureServices((context, services) =>
{
    services.AddTransient<IProcess, BuildProcess>();
    services.AddTransient<IProcess, TestProcess>();
});

using IHost host = builder.Build();

await host.RunAsync();