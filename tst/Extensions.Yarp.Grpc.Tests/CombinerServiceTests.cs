using System.Text.RegularExpressions;
using Grpc.Reflection.V1Alpha;

namespace Extensions.Yarp.Grpc.Tests
{
    public class CombinerServiceTests
    {
        [Fact]
        public void RemoveNotAllowedServices_Regex_AllAllowed()
        {
            var responses = new List<ServerReflectionResponse>()
            {
                new() {
                    ListServicesResponse = new ListServiceResponse{}
                }
            };
            responses[0].ListServicesResponse.Service.Add(new ServiceResponse { Name = "Service1" });
            responses[0].ListServicesResponse.Service.Add(new ServiceResponse { Name = "Service2" });

            var regex = new Regex("");
            CombinerService.RemoveNotAllowedServices(ref responses, regex);

            Assert.Equal(2, responses[0].ListServicesResponse.Service.Count);
            Assert.Equal("Service1", responses[0].ListServicesResponse.Service[0].Name);
            Assert.Equal("Service2", responses[0].ListServicesResponse.Service[1].Name);
        }
        [Fact]
        public void RemoveNotAllowedServices_Regex_Filters()
        {
            var responses = new List<ServerReflectionResponse>()
            {
                new() {
                    ListServicesResponse = new ListServiceResponse{}
                }
            };
            responses[0].ListServicesResponse.Service.Add(new ServiceResponse { Name = "Service1" });
            responses[0].ListServicesResponse.Service.Add(new ServiceResponse { Name = "Service2" });

            var regex = new Regex(".*1");
            CombinerService.RemoveNotAllowedServices(ref responses, regex);

            Assert.Single(responses[0].ListServicesResponse.Service);
            Assert.Equal("Service1", responses[0].ListServicesResponse.Service[0].Name);
        }
    }
}
