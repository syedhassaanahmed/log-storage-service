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

        private readonly IArchiveService _mockArchiveService = Substitute.For<IArchiveService>();
        private readonly IStorageService _mockStorageService = Substitute.For<IStorageService>();
        private readonly HttpClient _client;

        public LogsControllerTests()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>() // Borrow Startup logic we have from app under test
                .ConfigureServices(services => // but override services with mocks
                {
                    services
                        .AddTransient(serviceProvider => _mockArchiveService)
                        .AddSingleton(serviceProvider => _mockStorageService);
                });

            var server = new TestServer(webHostBuilder);
            _client = server.CreateClient();
        }

        #region UploadAsync
        
        [Fact]
        public async Task UploadAsync_EmptyBody_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PutAsync("/api/logs/filename.zip", 
                new StreamContent(Stream.Null));

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
                var response = await _client.PutAsync("/api/logs/somefile", new StreamContent(stream)
                {
                    Headers = { ContentLength = stream.Length}
                });

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
                var streamContent = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentLength = stream.Length,
                        ContentType = MediaTypeHeaderValue.Parse("application/txt")
                    }
                };

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
                var streamContent = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentLength = stream.Length,
                        ContentType = MediaTypeHeaderValue.Parse(CommonConstants.ZipContentType)
                    }
                };

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
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(true);

            using (var stream = File.Open($"{TestDataPath}/empty.zip", 
                FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentLength = stream.Length,
                        ContentType = MediaTypeHeaderValue.Parse(CommonConstants.ZipContentType)
                    }
                };

                // Act
                var response = await _client.PutAsync("/api/logs/empty.zip", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task UploadAsync_ZipFile_ReturnsCreatedWithLogFilesInfo()
        {
            // Arrange
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(false);
            _mockArchiveService.GetMetaData(Arg.Any<Stream>())
                .Returns(new[] {new MetaData {Name = "somefile.log" } });

            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentLength = stream.Length,
                        ContentType = MediaTypeHeaderValue.Parse(CommonConstants.ZipContentType)
                    }
                };

                // Act
                var response = await _client.PutAsync("/api/logs/20161215.zip", streamContent);
                var responseString = await response.Content.ReadAsStringAsync();
                JsonConvert.DeserializeObject<List<ArchiveResponse>>(responseString);

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal("/api/logs/20161215.zip", response.Headers.Location.PathAndQuery);
            }
        }

        #endregion

        #region GetMetaDataAsync

        [Fact]
        public async Task GetMetaDataAsync_IncorrectArchiveName_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/logs/file.zip");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMetaDataAsync_CorrectArchiveFileName_ReturnsOkWithLogFilesInfo()
        {
            // Arrange
            _mockStorageService.ExistsAsync(Arg.Any<string>()).Returns(true);
            _mockStorageService.GetMetaDataAsync(Arg.Any<string>())
                .Returns(new Dictionary<string, MetaData>
                {
                    {"file_1", new MetaData()}
                });

            // Act
            var response = await _client.GetAsync("/api/logs/file.zip");
            var responseString = await response.Content.ReadAsStringAsync();
            var filesInfo = JsonConvert.DeserializeObject<List<ArchiveResponse>>(responseString);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(filesInfo, file =>
                file.Url.EndsWith("/blob/file.zip/file_1"));
        }

        #endregion
    }
}
