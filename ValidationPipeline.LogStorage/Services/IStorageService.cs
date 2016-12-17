using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Services
{
    public interface IStorageService
    {
        Task UploadAsync(string archiveFileName, Stream archiveStream,
            IDictionary<string, MetaData> metaData);

        Task<bool> ExistsAsync(string archiveFileName);
        Task<IDictionary<string, MetaData>> GetMetaDataAsync(string archiveFileName);

        Task<MetaData> GetInnerFileMetaDataAsync(string archiveFileName,
            string innerFileName);

        Task<Stream> DownloadAsync(string archiveFileName);
    }
}
