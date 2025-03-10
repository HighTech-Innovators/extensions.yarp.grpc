using Grpc.Core;
using TestServerGreeterOne;

namespace TestServerGreeter.Services;

public class GreeterService : TestServerGreeterOne.TestServerGreeterOne.TestServerGreeterOneBase
{
    private readonly ILogger<GreeterService> _logger;
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHelloOne(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello one" + request.Name
        });
    }
}
