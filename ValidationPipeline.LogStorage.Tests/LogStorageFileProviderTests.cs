using Microsoft.Extensions.FileProviders;
using NSubstitute;
using ValidationPipeline.LogStorage.FileProviders;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    public class LogStorageFileProviderTests
    {
        private readonly IArchiveService _archiveService = Substitute.For<IArchiveService>();
        private readonly IStorageService _storageService = Substitute.For<IStorageService>();

        private readonly IFileProvider _fileProvider;

        public LogStorageFileProviderTests()
        {
            _fileProvider = new LogStorageFileProvider(_archiveService, _storageService);
        }

        [Fact]
        public void GetFileInfo_IncorrectPath_ReturnsNotFoundFileInfo()
        {
            // Arrange
            _storageService.ExistsAsync(Arg.Any<string>()).Returns(true);
            _storageService.GetInnerFileMetaDataAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new MetaData());

            // Act
            var result = _fileProvider.GetFileInfo("/some.zip");

            // Assert
            Assert.IsType<NotFoundFileInfo>(result);
        }

        [Fact]
        public void GetFileInfo_TooManyElementsInPath_ReturnsNotFoundFileInfo()
        {
            // Arrange
            _storageService.ExistsAsync(Arg.Any<string>()).Returns(true);
            _storageService.GetInnerFileMetaDataAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new MetaData());

            // Act
            var result = _fileProvider.GetFileInfo("/some.zip/thenanother/andagain");

            // Assert
            Assert.IsType<NotFoundFileInfo>(result);
        }

        [Fact]
        public void GetFileInfo_CorrectPathButFileDoesntExist_ReturnsNotFoundFileInfo()
        {
            // Arrange
            _storageService.ExistsAsync(Arg.Any<string>()).Returns(false);
            _storageService.GetInnerFileMetaDataAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new MetaData());

            // Act
            var result = _fileProvider.GetFileInfo("/some.zip/innerfile");

            // Assert
            Assert.IsType<NotFoundFileInfo>(result);
        }

        [Fact]
        public void GetFileInfo_CorrectPathButNoMetaData_ReturnsNotFoundFileInfo()
        {
            // Arrange
            _storageService.ExistsAsync(Arg.Any<string>()).Returns(true);
            _storageService.GetInnerFileMetaDataAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns((MetaData)null);

            // Act
            var result = _fileProvider.GetFileInfo("/some.zip/innerfile");

            // Assert
            Assert.IsType<NotFoundFileInfo>(result);
        }

        [Fact]
        public void GetFileInfo_CorrectPath_ReturnsLogStorageFileInfo()
        {
            // Arrange
            _storageService.ExistsAsync(Arg.Any<string>()).Returns(true);
            _storageService.GetInnerFileMetaDataAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new MetaData());

            // Act
            var result = _fileProvider.GetFileInfo("/some.zip/innerfile");

            // Assert
            Assert.IsType<LogStorageFileInfo>(result);
        }
    }
}
