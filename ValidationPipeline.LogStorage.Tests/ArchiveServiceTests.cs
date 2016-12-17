using System;
using System.IO;
using System.Linq;
using System.Text;
using ValidationPipeline.LogStorage.Services;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    public class ArchiveServiceTests
    {
        private const string TestDataPath = "TestData/ArchiveServiceTests";
        private readonly IArchiveService _archiveService = new ArchiveService();

        #region IsValid

        [Fact]
        public void IsValid_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => // Act
                _archiveService.IsValid(null));
        }

        [Fact]
        public void IsValid_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Act
                _archiveService.IsValid(stream);

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void IsValid_IncorrectFileFormat_ReturnsFalse()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Act
                var isValid = _archiveService.IsValid(stream);

                // Assert
                Assert.False(isValid);
            }
        }

        [Fact]
        public void IsValid_CorrectFileFormat_ReturnsTrue()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/empty.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var isValid = _archiveService.IsValid(stream);

                // Assert
                Assert.True(isValid);
            }
        }

        #endregion

        #region IsEmpty

        [Fact]
        public void IsEmpty_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => // Act
                _archiveService.IsEmpty(null));
        }

        [Fact]
        public void IsEmpty_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                _archiveService.IsEmpty(stream);

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void IsEmpty_FileWithContent_ReturnsFalse()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var isEmpty = _archiveService.IsEmpty(stream);

                // Assert
                Assert.False(isEmpty);
            }
        }

        [Fact]
        public void IsEmpty_FileWithoutContent_ReturnsTrue()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/empty.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var isEmpty = _archiveService.IsEmpty(stream);

                // Assert
                Assert.True(isEmpty);
            }
        }

        #endregion

        #region GetMetaData

        [Fact]
        public void GetMetaData_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => // Act
                _archiveService.GetMetaData(null));
        }

        [Fact]
        public void GetMetaData_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                _archiveService.GetMetaData(stream);

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void GetMetaData_Stream_ReturnsMetaData()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var result = _archiveService.GetMetaData(stream);

                // Assert
                Assert.Equal(3, result.Count());
            }
        }

        [Fact]
        public void GetMetaData_EmptyArchive_ReturnsEmptyCollection()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/empty.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var result = _archiveService.GetMetaData(stream);

                // Assert
                Assert.Empty(result);
            }
        }

        #endregion

        #region ExtractFile

        [Fact]
        public void ExtractFile_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => // Act
                _archiveService.ExtractInnerFile(null, "file.zip"));
        }

        [Fact]
        public void ExtractFile_EmptyFileName_ThrowsArgumentNullException()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Assert
                Assert.Throws<ArgumentNullException>(() => // Act
                    _archiveService.ExtractInnerFile(stream, string.Empty));
            }
        }

        [Fact]
        public void ExtractFile_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                _archiveService.ExtractInnerFile(stream, "something");

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void ExtractFile_IncorrectArchiveName_ReturnsNull()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var resultStream = _archiveService.ExtractInnerFile(stream, "something");

                // Assert
                Assert.Null(resultStream);
            }
        }

        [Fact]
        public void ExtractFile_CorrectArchiveName_ReturnsStream()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                // Act
                var resultStream = _archiveService.ExtractInnerFile(stream, "20161215T100001.log");

                // Assert
                Assert.NotNull(resultStream);
            }
        }

        #endregion
    }
}
