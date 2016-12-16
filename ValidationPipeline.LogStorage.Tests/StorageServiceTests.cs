using System;
using System.IO;
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
        private const string TestDataPath = "TestData/StorageServiceTests";
        private const string TestConnectionString = "UseDevelopmentStorage=true";

        private readonly IStorageService _storageService = new StorageService(TestConnectionString);

        [Fact]
        public async Task UploadAsync_EmptyFileName_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _storageService.UploadAsync(string.Empty, Stream.Null, null));
        }

        [Fact]
        public async Task UploadAsync_StreamNull_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _storageService.UploadAsync("somefile.zip", Stream.Null, null));
        }

        [Fact]
        public async Task UploadAsync_EmptyInnerFilesCollection_ThrowsArgumentNullException()
        {
            const string fileName = "20161215.zip";
            using (var stream = File.Open($"{TestDataPath}/{fileName}", FileMode.Open, FileAccess.Read))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await _storageService.UploadAsync(fileName, stream, null));
            }
        }
    }
}
