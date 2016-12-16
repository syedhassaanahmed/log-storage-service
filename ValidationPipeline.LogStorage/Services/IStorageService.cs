using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IStorageService
    {
        Task UploadAsync(string archiveFileName, Stream archiveStream,
            IList<string> innerFileNames);

        Task<bool> ExistsAsync(string archiveFileName);
        Task<IEnumerable<string>> GetInnerFileNamesAsync(string archiveFileName);
        Task<Stream> DownloadAsync(string archiveFileName);
    }
}
