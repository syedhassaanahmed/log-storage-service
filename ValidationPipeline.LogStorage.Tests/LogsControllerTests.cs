using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public async Task Upload_NoFileName_ReturnsNotFound()
        {
            // Act
            var response = await _client.PostAsync("/api/logs/", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Upload_FileNameWithoutZipExtension_ReturnsNotFound()
        {
            // Act
            var response = await _client.PostAsync("/api/logs/filename", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Upload_EmptyBody_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/logs/filename.zip", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Upload_NoContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            const string fileName = "20161215.zip";
            var stream = File.Open($"TestData/{fileName}", FileMode.Open, FileAccess.Read);

            // Act
            var response = await _client.PostAsync($"/api/logs/{fileName}", new StreamContent(stream));

            // Assert
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Upload_NonZipContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            const string fileName = "20161215.zip";
            var stream = File.Open($"TestData/{fileName}", FileMode.Open, FileAccess.Read);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/txt");

            // Act
            var response = await _client.PostAsync($"/api/logs/{fileName}", streamContent);

            // Assert
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }
    }
}
