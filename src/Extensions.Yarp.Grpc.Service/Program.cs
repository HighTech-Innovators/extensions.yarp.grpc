using GrpcCombinerTestProxy;
using GrpcCombinerTestProxy.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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
var routes = await yarpConfig.GetRoutes();
builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, yarpConfig.GetClusters());

var app = builder.Build();

// Use YARP reverse proxy
app.MapReverseProxy();

app.MapGrpcService<CombinerService>();


app.MapGet("/", () => "Hello World!");

app.Run();
