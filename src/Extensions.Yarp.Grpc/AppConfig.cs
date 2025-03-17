using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Extensions.Yarp.Grpc
{
    internal class AppConfig
    {
        internal List<string> Hosts { get; init; } = [];
        internal Regex AllowedServiceRegex { get; init; }

        public AppConfig(IConfiguration configuration)
        {
            var hostsConfig = configuration.GetSection("Hosts").Get<string[]>() ?? throw new Exception("Hosts must be provided");
            Hosts.AddRange(hostsConfig);
            var regexConfig = configuration.GetSection("AllowedServiceRegex").Get<string>() ?? throw new Exception("AllowedServiceRegex must be provided");
            AllowedServiceRegex = new Regex(regexConfig);
        }
    }
}
