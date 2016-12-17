using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ValidationPipeline.LogStorage.Extensions;
using ValidationPipeline.LogStorage.FileProviders;
using ValidationPipeline.LogStorage.Services;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    /// <summary>
    /// NOTE: These Tests require Azure Storage Emulator to be installed and running
    /// </summary>
    public class StorageServiceTests
    {
        private const string TestConnectionString = "UseDevelopmentStorage=true";

        private readonly IStorageService _storageService = new StorageService(TestConnectionString);

        [Fact]
        public void Constructor_EmptyConnectionString_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => // Act
                new StorageService(string.Empty));
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
            const string archiveFileName = "somefile.zip";

            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                for (var i = 0; i < 5; i++)
                {
                    // Act
                    await _storageService.UploadAsync(archiveFileName, stream, 
                        new[] { new LogStorageFileInfo() });

                    var exists = await _storageService.ExistsAsync(archiveFileName);

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
                await _storageService.GetMetaDataAsync(string.Empty));
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
            const string archiveFileName = "somefile.zip";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                await _storageService.UploadAsync(archiveFileName, stream,
                    new[] {new LogStorageFileInfo()});

                // Act
                var exists = await _storageService.ExistsAsync(archiveFileName);

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
            const string archiveFileName = "somefile.zip";
            var innerFileName = "somefile.log".ToBase64();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                await _storageService.UploadAsync(archiveFileName, stream,
                    new[] {new LogStorageFileInfo {Name = innerFileName}});

                // Act
                var metaData = await _storageService.GetMetaDataAsync(archiveFileName);

                // Assert
                Assert.NotNull(metaData);
                Assert.Single(metaData, fileInfo => fileInfo.Name == innerFileName);
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
            const string archiveFileName = "somefile.zip";
            const string expectedContent = "hello world";

            var encoding = Encoding.UTF8;

            using (var uploadStream = new MemoryStream(encoding.GetBytes(expectedContent)))
            {
                await _storageService.UploadAsync(archiveFileName, uploadStream,
                    new[] {new LogStorageFileInfo()});

                // Act
                var downloadStream = (MemoryStream)await _storageService.DownloadAsync(archiveFileName);
                var actualContent = encoding.GetString(downloadStream.ToArray());

                // Assert
                Assert.NotEqual(0, downloadStream.Length);
                Assert.Equal(expectedContent, actualContent);
            }
        }

        #endregion
    }
}
