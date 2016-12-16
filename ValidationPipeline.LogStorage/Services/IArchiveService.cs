using System.Collections.Generic;
using System.IO;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IArchiveService
    {
        bool IsValid(Stream archiveStream);
        bool IsEmpty(Stream archiveStream);
        IEnumerable<string> GetFileNames(Stream archiveStream);
        Stream ExtractFile(Stream archiveStream, string fileName);
    }
}
