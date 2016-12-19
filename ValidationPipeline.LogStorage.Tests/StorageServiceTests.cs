using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NSubstitute;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    /// <summary>
    /// NOTE: These Tests require Azure Storage Emulator to be installed and running
    /// </summary>
    public class StorageServiceTests
    {
        private readonly IStorageService _storageService;

        public StorageServiceTests()
        {
            var options = Substitute.For<IOptionsSnapshot<BlobStorageOptions>>();
            options.Value.Returns(new BlobStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            });

            _storageService = new StorageService(options);
        }

        #region UploadAsync

        [Fact]
        public async Task UploadAsync_EmptyArchiveName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.UploadAsync(string.Empty, Stream.Null, null));
        }

        [Fact]
        public async Task UploadAsync_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.UploadAsync("somefile.zip", Stream.Null, null));
        }

        [Fact]
        public async Task UploadAsync_EmptyMetaData_ThrowsArgumentNullException()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Assert
                await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                    await _storageService.UploadAsync("somefile.zip", stream, null));
            }
        }

        [Fact]
        public async Task UploadAsync_CorrectArchiveMultipleTimes_IsIdempotent()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                for (var i = 0; i < 5; i++)
                {
                    // Act
                    await _storageService.UploadAsync("somefile.zip", stream,
                        new Dictionary<string, MetaData>
                        {
                            {"file_100", new MetaData()}
                        });

                    var exists = await _storageService.ExistsAsync("somefile.zip");

                    // Assert
                    Assert.True(exists);
                }
            }
        }

        #endregion

        #region ExistsAsync

        [Fact]
        public async Task ExistsAsync_EmptyArchiveName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.ExistsAsync(string.Empty));
        }

        [Fact]
        public async Task ExistsAsync_IncorrectArchiveName_ReturnsFalse()
        {
            // Act
            var exists = await _storageService.ExistsAsync("somefile");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task ExistsAsync_CorrectArchiveName_ReturnsTrue()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                await _storageService.UploadAsync("somefile.zip", stream,
                    new Dictionary<string, MetaData>
                    {
                        {"something", new MetaData()}
                    });

                // Act
                var exists = await _storageService.ExistsAsync("somefile.zip");

                // Assert
                Assert.True(exists);
            }
        }

        #endregion

        #region GetMetaDataAsync

        [Fact]
        public async Task GetMetaDataAsync_EmptyArchiveName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.GetMetaDataAsync(string.Empty));
        }

        [Fact]
        public async Task GetMetaDataAsync_CorrectArchiveName_ReturnsMetaData()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                await _storageService.UploadAsync("somefile.zip", stream,
                    new Dictionary<string, MetaData>
                    {
                        {"file_1", new MetaData()}
                    });

                // Act
                var metaData = await _storageService.GetMetaDataAsync("somefile.zip");

                // Assert
                Assert.NotNull(metaData);
                Assert.Single(metaData, pair => pair.Key == "file_1");
            }
        }

        #endregion

        #region GetInnerFileMetaDataAsync

        [Fact]
        public async Task GetInnerFileMetaDataAsync_EmptyArchiveName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.GetInnerFileMetaDataAsync(string.Empty, "something"));
        }

        [Fact]
        public async Task GetInnerFileMetaDataAsync_EmptyInnerFileName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.GetInnerFileMetaDataAsync("something", string.Empty));
        }

        [Fact]
        public async Task GetInnerFileMetaDataAsync_CorrectArchiveName_ReturnsMetaData()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                await _storageService.UploadAsync("somefile.zip", stream,
                    new Dictionary<string, MetaData>
                    {
                        {"file_1", new MetaData {Name = "something"} }
                    });

                // Act
                var metaData = await _storageService.GetInnerFileMetaDataAsync("somefile.zip", "file_1");

                // Assert
                Assert.NotNull(metaData);
                Assert.Equal("something", metaData.Name);
            }
        }

        #endregion

        #region DownloadAsync

        [Fact]
        public async Task DownloadAsync_EmptyArchiveName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.DownloadAsync(string.Empty));
        }

        [Fact]
        public async Task DownloadAsync_CorrectArchiveName_ReturnsStream()
        {
            // Arrange
            var encoding = Encoding.UTF8;

            using (var uploadStream = new MemoryStream(encoding.GetBytes("hello world")))
            {
                await _storageService.UploadAsync("somefile.zip", uploadStream,
                    new Dictionary<string, MetaData>
                    {
                        {"something", new MetaData()}
                    });

                // Act
                var downloadStream = (MemoryStream)await _storageService.DownloadAsync("somefile.zip");
                var actualContent = encoding.GetString(downloadStream.ToArray());

                // Assert
                Assert.NotEqual(0, downloadStream.Length);
                Assert.Equal("hello world", actualContent);
            }
        }

        #endregion
    }
}
