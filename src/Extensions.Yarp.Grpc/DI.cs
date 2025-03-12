using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace Extensions.Yarp.Grpc
{
    public static class DI
    {
        public static void ConfigureServices(IServiceCollection builderServices)
        {
            builderServices.AddGrpc();
            builderServices.AddScoped<CombinerService>();
            builderServices.AddSingleton<YarpConfig>();

            // Add YARP services
            var logger = builderServices.BuildServiceProvider().GetRequiredService<ILogger<YarpConfig>>();
            var builderConfig = builderServices.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var yarpConfig = new YarpConfig(builderConfig, logger);
            var inMemory = new InMemoryConfigProvider(yarpConfig.GetRoutes().Result, yarpConfig.GetClusters());
            builderServices.AddSingleton<IProxyConfigProvider>(inMemory);
            builderServices.AddReverseProxy();
            builderServices.AddSingleton(inMemory);

            ////todo why doesnt this update the yarp config?
            //// Add YARP services
            //builderServices.AddReverseProxy();

            //builderServices.AddSingleton<IProxyConfigProvider>(serviceProvider =>
            //{
            //    var yarpConfig = serviceProvider.GetRequiredService<YarpConfig>();
            //    var inMemory = new InMemoryConfigProvider(yarpConfig.GetRoutes().Result, yarpConfig.GetClusters());
            //    return inMemory;
            //});
            //builderServices.AddSingleton(serviceProvider =>
            //{
            //    var yarpConfig = serviceProvider.GetRequiredService<YarpConfig>();
            //    return new InMemoryConfigProvider(yarpConfig.GetRoutes().Result, yarpConfig.GetClusters());
            //});

            builderServices.AddHostedService<ServiceMonitor>();
        }

        public static void ConfigureApp(WebApplication app)
        {
            // Use YARP reverse proxy
            app.MapReverseProxy();
            app.MapGrpcService<CombinerService>();
        }
    }
}
