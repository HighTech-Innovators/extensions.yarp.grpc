using Yarp.ReverseProxy.Configuration;

namespace Extensions.Yarp.Grpc.Service;

public class ServiceMonitor : BackgroundService
{
    private readonly YarpConfig yarpConfig;
    private readonly InMemoryConfigProvider inMemoryConfigProvider;
    public ServiceMonitor(YarpConfig yarpConfig, InMemoryConfigProvider inMemoryConfigProvider)
    {
        this.yarpConfig = yarpConfig;
        this.inMemoryConfigProvider = inMemoryConfigProvider;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var allServicesUp = await yarpConfig.AreAllServicesUp();
            if (allServicesUp)
            {
                inMemoryConfigProvider.Update(await yarpConfig.GetRoutes(), yarpConfig.GetClusters());
                Console.WriteLine("Updated yarp config, stopping");
                return;
            }
            Console.WriteLine("Some grpc services are down, retrying");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Check every x seconds
        }
    }
}
