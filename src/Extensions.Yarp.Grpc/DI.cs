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
        public static void AddAutoGrpcReverseProxy(this WebApplicationBuilder builder)
        {
            builder.Services.AddGrpc();
            builder.Services.AddSingleton<CombinerService>();
            builder.Services.AddSingleton<YarpConfig>();
            builder.Services.AddSingleton<AppConfig>();

            //Add YARP services
            builder.Services.AddReverseProxy();

            builder.Services.AddSingleton<IProxyConfigProvider>(s => s.GetRequiredService<InMemoryConfigProvider>());
            builder.Services.AddSingleton<InMemoryConfigProvider>(serviceProvider =>
            {
                var yarpConfig = serviceProvider.GetRequiredService<YarpConfig>();
                return new InMemoryConfigProvider(yarpConfig.GetRoutes().Result, yarpConfig.GetClusters());
            });

            builder.Services.AddHostedService<ServiceMonitor>();
        }

        public static void MapAutoGrpcReverseProxy(this WebApplication app)
        {
            // Use YARP reverse proxy
            app.MapReverseProxy();
            app.MapGrpcService<CombinerService>();
        }
    }
}
