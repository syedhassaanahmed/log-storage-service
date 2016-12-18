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

        #region Initialize

        [Fact]
        public void Initialize_StreamNull_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => // Act
                _archiveService.Initialize(null));
        }

        [Fact]
        public void Initialize_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Act
                _archiveService.Initialize(stream);

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void Initialize_StreamPositionNotZero_DoesNotThrowException()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                stream.Position = stream.Length - 1;

                // Act
                _archiveService.Initialize(stream);
            }
        }

        [Fact]
        public void Initialize_IncorrectFileFormat_ReturnsFalse()
        {
            // Arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world")))
            {
                // Act
                var initialized = _archiveService.Initialize(stream);

                // Assert
                Assert.False(initialized);
            }
        }

        [Fact]
        public void Initialize_CorrectFileFormat_ReturnsTrue()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/empty.zip", 
                FileMode.Open, FileAccess.Read))
            {
                // Act
                var initialized = _archiveService.Initialize(stream);

                // Assert
                Assert.True(initialized);
            }
        }

        #endregion

        #region IsEmpty

        [Fact]
        public void IsEmpty_Uninitialized_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(() => // Act
                _archiveService.IsEmpty());
        }

        [Fact]
        public void IsEmpty_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                _archiveService.IsEmpty();

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void IsEmpty_StreamPositionNotZero_DoesNotThrowException()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip",
                FileMode.Open, FileAccess.Read))
            {
                stream.Position = stream.Length - 1;
                _archiveService.Initialize(stream);

                // Act
                _archiveService.IsEmpty();
            }
        }

        [Fact]
        public void IsEmpty_FileWithContent_ReturnsFalse()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                var isEmpty = _archiveService.IsEmpty();

                // Assert
                Assert.False(isEmpty);
            }
        }

        [Fact]
        public void IsEmpty_FileWithoutContent_ReturnsTrue()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/empty.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                var isEmpty = _archiveService.IsEmpty();

                // Assert
                Assert.True(isEmpty);
            }
        }

        #endregion

        #region GetMetaData

        [Fact]
        public void GetMetaData_Uninitialized_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(() => // Act
                _archiveService.GetMetaData());
        }

        [Fact]
        public void GetMetaData_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                _archiveService.GetMetaData();

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void GetMetaData_StreamPositionNotZero_DoesNotThrowException()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                stream.Position = stream.Length - 1;
                _archiveService.Initialize(stream);

                // Act
                _archiveService.GetMetaData();
            }
        }

        [Fact]
        public void GetMetaData_Stream_ReturnsMetaData()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                var result = _archiveService.GetMetaData();

                // Assert
                Assert.Equal(3, result.Count());
            }
        }

        [Fact]
        public void GetMetaData_EmptyArchive_ReturnsEmptyCollection()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/empty.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                var result = _archiveService.GetMetaData();

                // Assert
                Assert.Empty(result);
            }
        }

        #endregion

        #region ExtractFile

        [Fact]
        public void ExtractFile_Uninitialized_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(() => // Act
                _archiveService.ExtractInnerFile("file.zip"));
        }

        [Fact]
        public void ExtractFile_EmptyFileName_ThrowsArgumentNullException()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip",
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Assert
                Assert.Throws<ArgumentNullException>(() => // Act
                    _archiveService.ExtractInnerFile(string.Empty));
            }
        }

        [Fact]
        public void ExtractFile_Stream_RemainsOpen()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                _archiveService.ExtractInnerFile("something");

                // Assert
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void ExtractFile_StreamPositionNotZero_DoesNotThrowException()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                stream.Position = stream.Length - 1;
                _archiveService.Initialize(stream);

                // Act
                _archiveService.ExtractInnerFile("something");
            }
        }

        [Fact]
        public void ExtractFile_IncorrectArchiveName_ReturnsNull()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                var resultStream = _archiveService.ExtractInnerFile("something");

                // Assert
                Assert.Null(resultStream);
            }
        }

        [Fact]
        public void ExtractFile_CorrectArchiveName_ReturnsStream()
        {
            // Arrange
            using (var stream = File.Open($"{TestDataPath}/20161215.zip", 
                FileMode.Open, FileAccess.Read))
            {
                _archiveService.Initialize(stream);

                // Act
                var resultStream = _archiveService.ExtractInnerFile("20161215T100001.log");

                // Assert
                Assert.NotNull(resultStream);
            }
        }

        #endregion
    }
}
