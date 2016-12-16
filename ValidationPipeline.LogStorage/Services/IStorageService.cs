using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IStorageService
    {
        Task<IEnumerable<string>> GetInnerFileNames(string archiveFileName);
        Task<bool> UploadAsync(string archiveFileName, Stream archiveStream, 
            IEnumerable<string> innerFileNames);
        Task<Stream> DownloadAsync(string archiveFileName);
    }
}
