using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
        public async Task UploadAsync_EmptyInnerFilesCollection_ThrowsArgumentNullException()
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
                    await _storageService.UploadAsync(archiveFileName, stream, new[] { "somefile.log" });
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
                await _storageService.GetInnerFileNamesAsync(string.Empty));
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
                await _storageService.UploadAsync(archiveFileName, stream, new[] { "somefile.log" });

                // Act
                var exists = await _storageService.ExistsAsync(archiveFileName);

                // Assert
                Assert.True(exists);
            }
        }

        #endregion

        #region GetInnerFileNamesAsync

        [Fact]
        public async Task GetInnerFileNamesAsync_EmptyArchiveName_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.GetInnerFileNamesAsync(string.Empty));
        }

        [Fact]
        public async Task GetInnerFileNamesAsync_CorrectArchiveName_ReturnsInnerFiles()
        {
            // Arrange
            const string archiveFileName = "somefile.zip";
            const string innerFileName = "somefile.log";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                await _storageService.UploadAsync(archiveFileName, stream, new[] { innerFileName });

                // Act
                var result = await _storageService.GetInnerFileNamesAsync(archiveFileName);

                // Assert
                Assert.NotNull(result);
                Assert.Single(result, resultFileName => resultFileName == innerFileName);
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
                await _storageService.UploadAsync(archiveFileName, uploadStream, new[] {"someinnerfile"});

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
