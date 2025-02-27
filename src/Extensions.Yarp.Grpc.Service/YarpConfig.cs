using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace GrpcCombinerTestProxy
{
    public class YarpConfig
    {
        public record GrpcService(string Service, string Host);

        public List<GrpcService> services = new List<GrpcService>();
        public YarpConfig(IConfiguration configuration)
        {
            var servicesConfig = configuration.GetSection("GrpcServices").GetChildren();
            foreach (var serviceConfig in servicesConfig)
            {
                var service = serviceConfig["Service"];
                var host = serviceConfig["Host"];
                if (service is null || host is null)
                {
                    throw new Exception("Service and Host must be provided for each GrpcService");
                }
                services.Add(new GrpcService(service, host));
            }
        }

        public IReadOnlyList<RouteConfig> GetRoutes()
        {
            var routes = new List<RouteConfig>();

            foreach (var service in services)
            {
                routes.Add(new RouteConfig
                {
                    RouteId = service.Service,
                    ClusterId = service.Service,
                    Match = new RouteMatch
                    {
                        Path = $"/{service.Service}/{{**catch-all}}"
                    }
                });
            }

            return routes;
        }

        public IReadOnlyList<ClusterConfig> GetClusters()
        {
            var clusters= new List<ClusterConfig>();

            foreach (var service in services)
            {
                clusters.Add(new ClusterConfig
                {
                    HttpRequest = new ForwarderRequestConfig
                    {
                        Version = new Version(2, 0),
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    },
                    ClusterId = service.Service,
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        { "destination1", new DestinationConfig { Address = service.Host } }
                    }
                });
            }

            return clusters;
        }
    }
}
