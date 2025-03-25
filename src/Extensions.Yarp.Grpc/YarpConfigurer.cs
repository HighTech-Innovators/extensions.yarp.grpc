using Grpc.Reflection.V1Alpha;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Extensions.Yarp.Grpc;

internal class YarpConfigurer
{
    private readonly YarpGrpcOptions yarpGrpcOptions;
    private readonly ILogger<YarpConfigurer> logger;
    private readonly CombinerService combinerService;

    public YarpConfigurer(IOptions<YarpGrpcOptions> yarpGrpcOptions,ILogger<YarpConfigurer> logger, CombinerService combinerService)
    {
        this.yarpGrpcOptions = yarpGrpcOptions.Value;
        this.logger = logger;
        this.combinerService = combinerService;
    }

    internal async Task<IReadOnlyList<RouteConfig>> GetRoutes()
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

    internal record ServicesToHost(string Host, List<string> ServiceNames);
    internal async Task<List<ServicesToHost>> GetServiceNamesFromReflection()
    {
        var result = new List<ServicesToHost>();
        foreach (var host in yarpGrpcOptions.Hosts)
        {
            try
            {
                var request = new ServerReflectionRequest();
                request.ListServices = "*";
                var responses = await combinerService.GetReflectionResponses(host, request);

                foreach (var response in responses)
                {
                    var names = response.ListServicesResponse.Service.Select(serviceResp => $"{serviceResp.Name}");
                    var noReflection = names.Where(name => !name.Contains("reflection"));

                    var filtered = noReflection.Where(name => new Regex(yarpGrpcOptions.AllowedServiceRegex).IsMatch(name));

                    result.Add(new ServicesToHost(host, filtered.ToList()));
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Path getting exception occured {e.Message}");
            }
        }
        return result;
    }

    internal async Task<bool> AreAllServicesUp()
    {
        try
        {
            foreach (var host in yarpGrpcOptions.Hosts)
            {
                var request = new ServerReflectionRequest();
                request.ListServices = "*";
                var responses = await combinerService.GetReflectionResponses(host, request);

            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal IReadOnlyList<ClusterConfig> GetClusters()
    {
        var clusters = new List<ClusterConfig>();

        foreach (var host in yarpGrpcOptions.Hosts)
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
