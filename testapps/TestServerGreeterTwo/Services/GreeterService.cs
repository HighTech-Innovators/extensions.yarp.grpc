using Grpc.Core;
using TestServerGreeterTwo;

namespace TestServerGreeter.Services;

public class GreeterService : TestServerGreeterTwo.TestServerGreeterTwo.TestServerGreeterTwoBase
{
    private readonly ILogger<GreeterService> _logger;
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHelloTwo(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello two" + request.Name
        });
    }
}
