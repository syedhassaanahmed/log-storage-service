using System.Collections.Generic;
using System.IO;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IArchiveService
    {
        bool Initialize(Stream archiveStream);
        bool IsEmpty();
        IEnumerable<MetaData> GetMetaData();
        Stream ExtractInnerFile(string innerFileName);
    }
}
