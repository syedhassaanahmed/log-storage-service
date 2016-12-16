using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        private const string TestDataPath = "TestData/LogsControllerTests";

        private readonly HttpClient _client;
        private readonly IArchiveService _mockArchiveService;
        private readonly IStorageService _mockStorageService;

        public LogsControllerTests()
        {
            _mockArchiveService = Substitute.For<IArchiveService>();
            _mockStorageService = Substitute.For<IStorageService>();

            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>() // Borrow Startup logic we have from app under test
                .ConfigureServices(services => // but override services with mocks
                {
                    services
                        .AddTransient(serviceProvider => _mockArchiveService)
                        .AddTransient(serviceProvider => _mockStorageService);
                });

            var server = new TestServer(webHostBuilder);
            _client = server.CreateClient();
        }

        #region UploadAsync Tests

        [Fact]
        public async Task UploadAsync_NoFileName_ReturnsNotFound()
        {
            // Act
            var response = await _client.PutAsync("/api/logs/", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        
        [Fact]
        public async Task UploadAsync_EmptyBody_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PutAsync("/api/logs/filename.zip", new StreamContent(Stream.Null));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadAsync_NoContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Act
                var response = await _client.PutAsync("/api/logs/somefile", new StreamContent(stream));

                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task UploadAsync_NonZipContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/txt");

                // Act
                var response = await _client.PutAsync("/api/logs/somefile", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task UploadAsync_NonZipFileWithZipContentType_ReturnsUnsupportedMediaType()
        {
            // Arrange
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(false);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

                // Act
                var response = await _client.PutAsync("/api/logs/somefile", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task UploadAsync_EmptyZipFile_ReturnsBadRequest()
        {
            // Arrange
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(false);

            const string archiveName = "empty.zip";
            using (var stream = File.Open($"{TestDataPath}/{archiveName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

                // Act
                var response = await _client.PutAsync($"/api/logs/{archiveName}", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task UploadAsync_ZipFile_ReturnsCreatedWithLogFileInfo()
        {
            // Arrange
            const string innerFileName = "somefile.log";
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.GetInnerFileNames(Arg.Any<Stream>())
                .Returns(new[] { innerFileName });

            _mockStorageService.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(),
                Arg.Any<IList<string>>()).Returns(true);

            const string archiveName = "20161215.zip";
            var route = $"/api/logs/{archiveName}";

            using (var stream = File.Open($"{TestDataPath}/{archiveName}", FileMode.Open, FileAccess.Read))
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

        [Fact]
        public async Task UploadAsync_ZipFileWhenStorageError_ReturnsInternalServerError()
        {
            // Arrange
            const string innerFileName = "somefile.log";
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.GetInnerFileNames(Arg.Any<Stream>())
                .Returns(new[] { innerFileName });

            _mockStorageService.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), 
                Arg.Any<IList<string>>()).Returns(false);

            const string archiveName = "20161215.zip";
            var route = $"/api/logs/{archiveName}";

            using (var stream = File.Open($"{TestDataPath}/{archiveName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

                // Act
                var response = await _client.PutAsync(route, streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        #endregion
    }
}
