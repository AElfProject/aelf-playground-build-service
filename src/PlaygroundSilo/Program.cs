using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleans((context, silo) =>
    {
        silo.UseZooKeeperClustering(options =>
        {
            // for development, start services using docker compose.
            options.ConnectionString = Environment.GetEnvironmentVariable("ASPNETCORE_ZOOKEEPER_HOST") ?? "localhost:2181";
        });

        var POD_NAMESPACE = Environment.GetEnvironmentVariable("POD_NAMESPACE"); // in kubernetes, this will not be empty
        if (!string.IsNullOrEmpty(POD_NAMESPACE)) // only use kubernetes hosting when running in kubernetes
        {
            silo.UseKubernetesHosting();
        }

        silo.ConfigureLogging(logging => logging.AddConsole());
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();

await host.RunAsync();