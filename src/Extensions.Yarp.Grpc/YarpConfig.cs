using Grpc.Reflection.V1Alpha;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Extensions.Yarp.Grpc;

public class YarpConfig
{
    private readonly ILogger<YarpConfig> logger;

    public List<string> Hosts { get; init; } = [];
    public Regex AllowedServiceRegex { get; init; }

    public YarpConfig(IConfiguration configuration, ILogger<YarpConfig> logger)
    {
        var hostsConfig = configuration.GetSection("Hosts").Get<string[]>() ?? throw new Exception("Hosts must be provided");
        Hosts.AddRange(hostsConfig);
        var regexConfig = configuration.GetSection("AllowedServiceRegex").Get<string>() ?? throw new Exception("AllowedServiceRegex must be provided");
        AllowedServiceRegex = new Regex(regexConfig);
        this.logger = logger;
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
                logger.LogError($"Path getting exception occured {e.Message}");
            }
        }
        return result;
    }

    public async Task<bool> AreAllServicesUp()
    {
        try
        {
            foreach (var host in Hosts)
            {
                var request = new ServerReflectionRequest();
                request.ListServices = "*";
                var responses = await CombinerService.GetReflectionResponses(host, request);

            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
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
