using System;
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
using ValidationPipeline.LogStorage.Extensions;
using ValidationPipeline.LogStorage.FileProviders;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Tests
{
    public class LogsControllerTests
    {
        private const string ZipContentType = "application/zip";
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
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(ZipContentType);

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

            const string archiveFileName = "empty.zip";
            using (var stream = File.Open($"{TestDataPath}/{archiveFileName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(ZipContentType);

                // Act
                var response = await _client.PutAsync($"/api/logs/{archiveFileName}", streamContent);

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task UploadAsync_ZipFile_ReturnsCreatedWithLogFilesInfo()
        {
            // Arrange
            const string innerFileName = "somefile.log";
            _mockArchiveService.IsValid(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.IsEmpty(Arg.Any<Stream>()).Returns(true);
            _mockArchiveService.GetMetaData(Arg.Any<Stream>())
                .Returns(new[] {new LogStorageFileInfo {Name = innerFileName}});

            const string archiveFileName = "20161215.zip";
            var route = $"/api/logs/{archiveFileName}";

            using (var stream = File.Open($"{TestDataPath}/{archiveFileName}", FileMode.Open, FileAccess.Read))
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(ZipContentType);

                // Act
                var response = await _client.PutAsync(route, streamContent);
                var responseString = await response.Content.ReadAsStringAsync();
                var filesInfo = JsonConvert.DeserializeObject<List<ArchiveResponse>>(responseString);
                var base64FileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(innerFileName));

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal(route, response.Headers.Location.PathAndQuery);
                Assert.Single(filesInfo, file => 
                    file.Url.EndsWith($"{archiveFileName.Replace(".", "_")}/{base64FileName}"));
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
            var innerFileName = "somefile.log".ToBase64();

            _mockStorageService.ExistsAsync(Arg.Any<string>()).Returns(true);
            _mockStorageService.GetMetaDataAsync(Arg.Any<string>())
                .Returns(new[] { new LogStorageFileInfo { Name = innerFileName } });

            const string archiveFileName = "file.zip";
            var route = $"/api/logs/{archiveFileName}";

            // Act
            var response = await _client.GetAsync(route);
            var responseString = await response.Content.ReadAsStringAsync();
            var filesInfo = JsonConvert.DeserializeObject<List<ArchiveResponse>>(responseString);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(filesInfo, file =>
                file.Url.EndsWith($"{archiveFileName.Replace(".", "_")}/{innerFileName}"));
        }

        #endregion

        #region DownloadAsync


        #endregion
    }
}
