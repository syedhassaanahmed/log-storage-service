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
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                for (var i = 0; i < 10; i++)
                {
                    // Act
                    var isUploaded = await _storageService.UploadAsync("somefile.zip", stream, 
                        new[] { "somefile.log" });

                    // Assert
                    Assert.True(isUploaded);
                }
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
        public async Task GetInnerFileNamesAsync_IncorrectArchiveName_ReturnsNull()
        {
            // Act
            var result = await _storageService.GetInnerFileNamesAsync("somefile");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetInnerFileNamesAsync_CorrectArchiveName_ReturnsInnerFiles()
        {
            // Arrange
            const string archiveName = "somefile.zip";
            const string innerFileName = "somefile.log";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                var isUploaded = await _storageService.UploadAsync(archiveName, stream,
                    new[] { innerFileName });

                // Act
                var result = await _storageService.GetInnerFileNamesAsync(archiveName);

                // Assert
                Assert.True(isUploaded);
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
                await _storageService.DownloadAsync(string.Empty, new MemoryStream()));
        }

        [Fact]
        public async Task DownloadAsync_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => // Act
                await _storageService.DownloadAsync("somefile.zip", Stream.Null));
        }

        [Fact]
        public async Task DownloadAsync_IncorrectArchiveName_ReturnsFalse()
        {
            // Act
            var isDownloaded = await _storageService.DownloadAsync("somefile", new MemoryStream());

            // Assert
            Assert.False(isDownloaded);
        }

        [Fact]
        public async Task DownloadAsync_CorrectArchiveName_SetsStream()
        {
            // Arrange
            const string archiveName = "somefile.zip";
            const string expectedContent = "hello world";

            using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent)))
            {
                var isUploaded = await _storageService.UploadAsync(archiveName, uploadStream, 
                    new[] {"someinnerfile"});

                using (var downloadStream = new MemoryStream())
                {
                    // Act
                    var isDownloaded = await _storageService.DownloadAsync(archiveName, downloadStream);
                    var actualContent = Encoding.UTF8.GetString(downloadStream.ToArray());

                    // Assert
                    Assert.True(isUploaded);
                    Assert.True(isDownloaded);
                    Assert.NotEqual(0, downloadStream.Length);
                    Assert.Equal(expectedContent, actualContent);
                }
            }
        }

        #endregion
    }
}
