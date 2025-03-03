using Grpc.Reflection.V1Alpha;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using System.Linq;
using GrpcCombinerTestProxy.Services;
using System.Text.RegularExpressions;

namespace GrpcCombinerTestProxy
{
    public class YarpConfig
    {
        public record GrpcService(string Service, string Host);
        public List<string> Hosts { get; init; } = [];
        public Regex AllowedServiceRegex { get; init; }
        public YarpConfig(IConfiguration configuration)
        {
            var hostsConfig = configuration.GetSection("Hosts").Get<string[]>() ?? throw new Exception("Hosts must be provided");
            Hosts.AddRange(hostsConfig);
            var regexConfig = configuration.GetSection("AllowedServiceRegex").Get<string>() ?? throw new Exception("AllowedServiceRegex must be provided");
            AllowedServiceRegex = new Regex(regexConfig);
        }

        public async Task<IReadOnlyList<RouteConfig>> GetRoutes()
        {
            var routes = new List<RouteConfig>();
            var servicesToHosts = await GetServiceNamesFromReflection();

            foreach (var servicesToHost in servicesToHosts)
            {
                foreach (var service in servicesToHost.ServiceNames)
                {
                    routes.Add(new RouteConfig
                    {
                        RouteId = "route-" + servicesToHost.Host + "-" + service,
                        ClusterId = "cluster-" + servicesToHost.Host,
                        Match = new RouteMatch
                        {
                            Path = $"/{service}/{{**catch-all}}"
                        }
                    });
                }
            }
            return routes;
        }

        public record ServicesToHost(string Host, List<string> ServiceNames);
        public async Task<List<ServicesToHost>> GetServiceNamesFromReflection()
        {
            var result = new List<ServicesToHost>();
            foreach (var host in Hosts)
            {
                try
                {
                    var request = new ServerReflectionRequest();
                    request.ListServices = "*";
                    var responses = await CombinerService.GetReflectionResponses(host, request);

                    foreach (var response in responses)
                    {
                        var names = response.ListServicesResponse.Service.Select(serviceResp => $"{serviceResp.Name}");
                        var noReflection = names.Where(name => !name.Contains("reflection"));

                        var filtered = noReflection.Where(name => AllowedServiceRegex.IsMatch(name));

                        result.Add(new ServicesToHost(host, filtered.ToList()));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Path getting exception occured {e}");
                }
            }
            return result;
        }

        public IReadOnlyList<ClusterConfig> GetClusters()
        {
            var clusters = new List<ClusterConfig>();

            foreach (var host in Hosts)
            {
                clusters.Add(new ClusterConfig
                {
                    HttpRequest = new ForwarderRequestConfig
                    {
                        Version = new Version(2, 0),
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    },
                    ClusterId = "cluster-" + host,
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        { "destination1", new DestinationConfig { Address = host } }
                    }
                });
            }

            return clusters;
        }
    }
}
