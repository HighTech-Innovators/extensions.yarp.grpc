using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace Extensions.Yarp.Grpc;

internal class ServiceMonitor : BackgroundService
{
    private readonly YarpConfig yarpConfig;
    private readonly InMemoryConfigProvider inMemoryConfigProvider;
    private readonly ILogger<ServiceMonitor> logger;

    public ServiceMonitor(YarpConfig yarpConfig, InMemoryConfigProvider inMemoryConfigProvider, ILogger<ServiceMonitor> logger)
    {
        this.yarpConfig = yarpConfig;
        this.inMemoryConfigProvider = inMemoryConfigProvider;
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var allServicesUp = await yarpConfig.AreAllServicesUp();
            if (allServicesUp)
            {
                inMemoryConfigProvider.Update(await yarpConfig.GetRoutes(), yarpConfig.GetClusters());
                logger.LogInformation("Updated yarp config, stopping retrying");
                return;
            }
            logger.LogWarning("Some grpc services are down, retrying");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Check every x seconds
        }
    }
}
