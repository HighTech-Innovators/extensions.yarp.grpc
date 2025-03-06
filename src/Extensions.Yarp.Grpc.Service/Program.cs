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

Extensions.Yarp.Grpc.DI.ConfigureServices(builder.Services);

var app = builder.Build();

Extensions.Yarp.Grpc.DI.ConfigureApp(app);

app.Run();
