using Grpc.Core;
using TestServerGreeter;

namespace TestServerGreeter.Services;

public class GreeterService : TestServerGreeter.TestServerGreeter.TestServerGreeterBase
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
