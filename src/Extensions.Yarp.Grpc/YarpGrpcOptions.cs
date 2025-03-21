using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Extensions.Yarp.Grpc
{
    public class YarpGrpcOptions
    {
        public const string YarpGrpc= "YarpGrpc";
        public List<string> Hosts { get; set; } = [];
        public string AllowedServiceRegex { get; set; }="";
    }
}
