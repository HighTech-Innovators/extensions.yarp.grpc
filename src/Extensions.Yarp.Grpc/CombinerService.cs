﻿using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Extensions.Yarp.Grpc;
internal class CombinerService : ServerReflection.ServerReflectionBase
{
    private readonly AppConfig appConfig;
    private readonly ILogger<CombinerService> logger;
    private const int CHANNEL_TIMEOUT_MILLIS = 500;
    private readonly Dictionary<string,GrpcChannel> channels= [];

    public CombinerService(AppConfig appConfig, ILogger<CombinerService> logger)
    {
        this.appConfig = appConfig;
        this.logger = logger;
        var hosts= appConfig.Hosts;
        foreach (var host in hosts)
        {
            channels[host] = GrpcChannel.ForAddress(host, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMilliseconds(CHANNEL_TIMEOUT_MILLIS)
                }
            });
        }
    }
    public override async Task ServerReflectionInfo(IAsyncStreamReader<ServerReflectionRequest> requestStream, IServerStreamWriter<ServerReflectionResponse> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            var returnedResponses = new List<ServerReflectionResponse>();
            foreach (var host in appConfig.Hosts)
            {
                try
                {
                    var responses = await GetReflectionResponses(host, request);

                    RemoveNotAllowedServices(ref responses, appConfig.AllowedServiceRegex);

                    returnedResponses.AddRange(responses);
                }
                catch (Exception e)
                {
                    logger.LogError($"Exception occured while getting reflection response for host {host}: {e.Message}");
                }
            }

            var combinedResponses = CombineResponses(returnedResponses);
            await responseStream.WriteAsync(combinedResponses);
        }
    }

    internal static void RemoveNotAllowedServices(ref List<ServerReflectionResponse> responses, Regex allowedServiceRegex)
    {
        for (int i = 0; i < responses.Count; i++)
        {
            var response = responses[i];
            if (response == null || response.ListServicesResponse == null || response.ListServicesResponse.Service == null)
            {
                continue;
            }
            for (int j = 0; j < response.ListServicesResponse.Service.Count; j++)
            {
                var service = response.ListServicesResponse.Service[j];
                if (service == null)
                {
                    continue;
                }
                if (!allowedServiceRegex.IsMatch(service.Name))
                {
                    response.ListServicesResponse.Service.RemoveAt(j);
                    j--;
                }
            }
        }
    }

    internal async Task<List<ServerReflectionResponse>> GetReflectionResponses(string adr, ServerReflectionRequest request)
    {
        var channel = channels[adr];
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
