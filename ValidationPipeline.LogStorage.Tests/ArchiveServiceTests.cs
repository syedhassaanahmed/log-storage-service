using System;
using System.IO;
using System.Linq;
using ValidationPipeline.LogStorage.Services;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    public class ArchiveServiceTests
    {
        private readonly IArchiveService _archiveService = new ArchiveService();

        #region IsValid

        [Fact]
        public void IsValid_StreamNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _archiveService.IsValid(null));
        }

        [Fact]
        public void IsValid_Stream_RemainsOpen()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                _archiveService.IsValid(stream);
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void IsValid_IncorrectFileFormat_ReturnsFalse()
        {
            using (var stream = File.Open("TestData/non-zip-file.txt", FileMode.Open, FileAccess.Read))
            {
                Assert.False(_archiveService.IsValid(stream));
            }
        }

        [Fact]
        public void IsValid_CorrectFileFormat_ReturnsTrue()
        {
            using (var stream = File.Open("TestData/empty.zip", FileMode.Open, FileAccess.Read))
            {
                Assert.True(_archiveService.IsValid(stream));
            }
        }

        #endregion

        #region IsEmpty

        [Fact]
        public void IsEmpty_StreamNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _archiveService.IsEmpty(null));
        }

        [Fact]
        public void IsEmpty_Stream_RemainsOpen()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                _archiveService.IsEmpty(stream);
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void IsEmpty_FileWithContent_ReturnsFalse()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                Assert.False(_archiveService.IsEmpty(stream));
            }
        }

        [Fact]
        public void IsEmpty_FileWithoutContent_ReturnsTrue()
        {
            using (var stream = File.Open("TestData/empty.zip", FileMode.Open, FileAccess.Read))
            {
                Assert.True(_archiveService.IsEmpty(stream));
            }
        }

        #endregion

        #region GetInnerFileNames

        [Fact]
        public void GetInnerFileNames_StreamNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _archiveService.GetInnerFileNames(null));
        }

        [Fact]
        public void GetInnerFileNames_Stream_RemainsOpen()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                _archiveService.GetInnerFileNames(stream);
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void GetInnerFileNames_Stream_ReturnsFileNames()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                var result = _archiveService.GetInnerFileNames(stream);
                Assert.Equal(3, result.Count());
            }
        }

        [Fact]
        public void GetInnerFileNames_EmptyArchive_ReturnsEmptyCollection()
        {
            using (var stream = File.Open("TestData/empty.zip", FileMode.Open, FileAccess.Read))
            {
                var result = _archiveService.GetInnerFileNames(stream);
                Assert.Empty(result);
            }
        }

        #endregion

        #region ExtractFile

        [Fact]
        public void ExtractFile_StreamNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _archiveService.ExtractFile(null, "file.zip"));
        }

        [Fact]
        public void ExtractFile_EmptyFileName_ThrowsArgumentNullException()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<ArgumentNullException>(() => _archiveService.ExtractFile(stream, string.Empty));
            }
        }

        [Fact]
        public void ExtractFile_Stream_RemainsOpen()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                _archiveService.ExtractFile(stream, "something");
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void ExtractFile_WrongFileName_ReturnsNull()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                var resultStream = _archiveService.ExtractFile(stream, "something");
                Assert.Null(resultStream);
            }
        }

        [Fact]
        public void ExtractFile_CorrectFileName_ReturnsStream()
        {
            using (var stream = File.Open("TestData/20161215.zip", FileMode.Open, FileAccess.Read))
            {
                var resultStream = _archiveService.ExtractFile(stream, "20161215T100001.log");
                Assert.NotNull(resultStream);
            }
        }

        #endregion
    }
}
