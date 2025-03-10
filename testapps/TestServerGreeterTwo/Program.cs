using Microsoft.AspNetCore.Server.Kestrel.Core;
using TestServerGreeter.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8082, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.MapGrpcReflectionService();

app.Run();
