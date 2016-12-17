using System.Collections.Generic;
using System.IO;
using ValidationPipeline.LogStorage.FileProviders;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IArchiveService
    {
        bool IsValid(Stream archiveStream);
        bool IsEmpty(Stream archiveStream);
        IEnumerable<LogStorageFileInfo> GetMetaData(Stream archiveStream);
        Stream ExtractInnerFile(Stream archiveStream, string innerFileName);
    }
}
