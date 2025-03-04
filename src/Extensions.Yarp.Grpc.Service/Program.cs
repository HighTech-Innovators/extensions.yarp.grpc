using Extensions.Yarp.Grpc.Service;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});


builder.Services.AddGrpc();
builder.Services.AddScoped<CombinerService>();
builder.Services.AddSingleton<YarpConfig>();

// Add YARP services
var yarpConfig = new YarpConfig(builder.Configuration);
var inMemory=new InMemoryConfigProvider(yarpConfig.GetRoutes().Result, yarpConfig.GetClusters());
builder.Services.AddSingleton<IProxyConfigProvider>(inMemory);
builder.Services.AddReverseProxy();
builder.Services.AddSingleton(inMemory);
builder.Services.AddHostedService<ServiceMonitor>();

var app = builder.Build();

// Use YARP reverse proxy
app.MapReverseProxy();

app.MapGrpcService<CombinerService>();


app.MapGet("/", () => "Hello World!");

app.Run();
