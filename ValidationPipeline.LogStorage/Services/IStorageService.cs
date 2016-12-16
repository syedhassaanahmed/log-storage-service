using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IStorageService
    {
        Task<bool> UploadAsync(string archiveFileName, Stream archiveStream,
            IList<string> innerFileNames);

        Task<IEnumerable<string>> GetInnerFileNamesAsync(string archiveFileName);
        Task<bool> DownloadAsync(string archiveFileName, Stream targetStream);
    }
}
