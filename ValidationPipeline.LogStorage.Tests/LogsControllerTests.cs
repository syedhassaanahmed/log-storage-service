using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    public class LogsControllerTests
    {
        private readonly HttpClient _client;

        public LogsControllerTests()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();

            var server = new TestServer(webHostBuilder);
            _client = server.CreateClient();
        }

        [Fact]
        public async Task Upload_EmptyBody_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/logs", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
