using System.Text.RegularExpressions;
using Extensions.Yarp.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();


// Configure Kestrel to use HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Configuration.AddJsonFile("/config/yarpgrpc.json", optional: true, reloadOnChange: true);

//builder.AddAutoGrpcReverseProxy(new YarpGrpcOptions
//{
//    Hosts = ["http://localhost:10085"],
//    AllowedServiceRegex = new Regex("")
//});
builder.AddAutoGrpcReverseProxy();

var app = builder.Build();

app.MapAutoGrpcReverseProxy();

app.Run();
