using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ValidationPipeline.LogStorage.FileProviders;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IStorageService
    {
        Task UploadAsync(string archiveFileName, Stream archiveStream,
            IList<LogStorageFileInfo> metaData);

        Task<bool> ExistsAsync(string archiveFileName);
        Task<bool> InnerFileExistsAsync(string archiveFileName, string innerFileName);
        Task<IEnumerable<LogStorageFileInfo>> GetMetaDataAsync(string archiveFileName);
        Task<Stream> DownloadAsync(string archiveFileName);
    }
}
