using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using ValidationPipeline.LogStorage.Services;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Tests
{
    public class LogsControllerTests
    {
        private readonly HttpClient _client;
        private readonly IArchiveService _mockArchiveService;

        public LogsControllerTests()
        {
            _mockArchiveService = Substitute.For<IArchiveService>();

            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureServices(services =>
                {
                    services.AddTransient(s => _mockArchiveService);
                });

            var server = new TestServer(webHostBuilder);
            _client = server.CreateClient();
        }

        #region Upload Tests

        [Fact]
        public async Task Upload_NoFileName_ReturnsNotFound()
        {
            // Act
            var response = await _client.PutAsync("/api/logs/", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        
        [Fact]
        public async Task Upload_EmptyBody_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PutAsync("/api/logs/filename.zip", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Upload_NoContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            const string fileName = "20161215.zip";
            using (var stream = File.Open($"TestData/{fileName}", FileMode.Open, FileAccess.Read))
            {
                // Act
                var response = await _client.PutAsync($"/api/logs/{fileName}", new StreamContent(stream));

                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Upload_NonZipContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            const string fileName = "20161215.zip";
            using (var stream = File.Open($"TestData/{fileName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/txt");

                // Act
                var response = await _client.PutAsync($"/api/logs/{fileName}", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Upload_NonZipFileWithZipContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(false);

            const string fileName = "non-zip-file.txt";
            using (var stream = File.Open($"TestData/{fileName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

                // Act
                var response = await _client.PutAsync("/api/logs/somefile.zip", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Upload_EmptyZipFile_ReturnsBadRequest()
        {
            // Arrange
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(false);

            const string fileName = "empty.zip";
            using (var stream = File.Open($"TestData/{fileName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

                // Act
                var response = await _client.PutAsync($"/api/logs/{fileName}", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task Upload_ZipFile_ReturnsCreatedWithLogFileInfo()
        {
            // Arrange
            const string innerFileName = "somefile.log";
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.GetInnerFileNames(Arg.Any<Stream>())
                .Returns(new[] { innerFileName });

            const string archiveName = "20161215.zip";
            var route = $"/api/logs/{archiveName}";

            using (var stream = File.Open($"TestData/{archiveName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

                // Act
                var response = await _client.PutAsync(route, streamContent);
                var responseString = await response.Content.ReadAsStringAsync();
                var filesInfo = JsonConvert.DeserializeObject<List<LogFileInfo>>(responseString);

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal(route, response.Headers.Location.PathAndQuery);
                Assert.Single(filesInfo, file => 
                    file.Url.EndsWith($"{route}/{innerFileName}"));
            }
        }

        #endregion
    }
}
