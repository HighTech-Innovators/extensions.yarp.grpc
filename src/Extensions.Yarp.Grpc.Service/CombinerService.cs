using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcCombinerTestProxy.Services;
public class CombinerService : ServerReflection.ServerReflectionBase
{
    private readonly YarpConfig yarpConfig;

    public CombinerService(YarpConfig yarpConfig)
    {
        this.yarpConfig = yarpConfig;
    }
    public override async Task ServerReflectionInfo(IAsyncStreamReader<ServerReflectionRequest> requestStream, IServerStreamWriter<ServerReflectionResponse> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            var hosts = yarpConfig.services.Select(s => s.Host);
            //var tasks = hosts.Select(adr => GetReflectionResponses(adr, requests));
            //var responsesArr= await Task.WhenAll(tasks);
            //var responses = responsesArr.SelectMany(r => r).ToList();

            var responses = new List<ServerReflectionResponse>();
            foreach (var host in hosts)
            {
                try
                {
                    var res = await GetReflectionResponses(host, request);
                    responses.AddRange(res);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception occured {e}");
                }
            }

            var combinedResponses = CombineResponses(responses);
            await responseStream.WriteAsync(combinedResponses);
        }
    }

    private async Task<List<ServerReflectionResponse>> GetReflectionResponses(string adr, ServerReflectionRequest request)
    {
        var channel = GrpcChannel.ForAddress(adr);
        var client = new ServerReflection.ServerReflectionClient(channel);

        var responses = new List<ServerReflectionResponse>();
        using var call = client.ServerReflectionInfo();
        await call.RequestStream.WriteAsync(request);
        await call.RequestStream.CompleteAsync();

        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            responses.Add(response);
        }
        return responses;
    }

    private ServerReflectionResponse CombineResponses(List<ServerReflectionResponse> responses)
    {
        var combinedResponse = new ServerReflectionResponse();

        foreach (var response in responses)
        {
            if (response == null)
            {
                continue;
            }
            if (response.FileDescriptorResponse != null)
            {
                combinedResponse.FileDescriptorResponse ??= new FileDescriptorResponse();
                combinedResponse.FileDescriptorResponse.FileDescriptorProto.AddRange(response.FileDescriptorResponse.FileDescriptorProto);
            }

            if (response.AllExtensionNumbersResponse != null)
            {
                combinedResponse.AllExtensionNumbersResponse ??= new ExtensionNumberResponse();
                combinedResponse.AllExtensionNumbersResponse.ExtensionNumber.AddRange(response.AllExtensionNumbersResponse.ExtensionNumber);
            }

            if (response.ListServicesResponse != null)
            {
                combinedResponse.ListServicesResponse ??= new ListServiceResponse();
                combinedResponse.ListServicesResponse.Service.AddRange(response.ListServicesResponse.Service);
            }
        }

        return combinedResponse;
    }
}
