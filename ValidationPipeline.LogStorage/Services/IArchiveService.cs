using System.Collections.Generic;
using System.IO;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IArchiveService
    {
        bool IsValid(Stream archiveStream);
        bool IsEmpty(Stream archiveStream);
        IEnumerable<MetaData> GetMetaData(Stream archiveStream);
        Stream ExtractInnerFile(Stream archiveStream, string innerFileName);
    }
}
