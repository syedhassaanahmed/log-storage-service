using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using ValidationPipeline.LogStorage.FileProviders;

namespace ValidationPipeline.LogStorage.Services
{
    public class StorageService : IStorageService
    {
        private const string ContainerName = "validationpipeline";
        private const string MetaDataKeyPrefix = "file_";

        private readonly CloudBlobContainer _blobContainer;

        public StorageService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            _blobContainer = blobClient.GetContainerReference(ContainerName);
            _blobContainer.CreateIfNotExistsAsync().Wait();
        }

        #region IStorageService Implementation

        public async Task UploadAsync(string archiveFileName, Stream archiveStream,
            IList<LogStorageFileInfo> metaData)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            if (metaData == null || !metaData.Any())
                throw new ArgumentNullException(nameof(metaData));

            var blockBlob = _blobContainer.GetBlockBlobReference(archiveFileName);
            StoreMetaData(metaData, blockBlob);

            await blockBlob.UploadFromStreamAsync(archiveStream);
        }

        public async Task<bool> ExistsAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            return await blob.ExistsAsync();
        }

        public async Task<bool> InnerFileExistsAsync(string archiveFileName, string innerFileName)
        {
            var archiveExists = await ExistsAsync(archiveFileName);
            if (!archiveExists)
                return false;

            if (string.IsNullOrWhiteSpace(innerFileName))
                throw new ArgumentNullException(nameof(innerFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            await blob.FetchAttributesAsync();

            return blob.Metadata.ContainsKey(MetaDataKeyPrefix + innerFileName);
        }

        public async Task<IEnumerable<LogStorageFileInfo>> GetMetaDataAsync(string archiveFileName)
        {
            if(string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            await blob.FetchAttributesAsync();
            return RetrieveMetaData(blob);
        }

        public async Task<Stream> DownloadAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blockBlob = _blobContainer.GetBlockBlobReference(archiveFileName);
            var memoryStream = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(memoryStream);

            return memoryStream;
        }

        #endregion

        #region Helpers

        private static void StoreMetaData(IEnumerable<LogStorageFileInfo> metaData, 
            CloudBlob blockBlob)
        {
            foreach (var fileInfo in metaData)
            {
                blockBlob.Metadata.Add(MetaDataKeyPrefix + fileInfo.Name, 
                    JsonConvert.SerializeObject(fileInfo));
            }
        }

        private static IEnumerable<LogStorageFileInfo> RetrieveMetaData(CloudBlob blockBlob)
        {
            if (blockBlob.Metadata.Count == 0)
                return Enumerable.Empty<LogStorageFileInfo>();

            return blockBlob.Metadata.Keys.Where(key => key.StartsWith(MetaDataKeyPrefix))
                .Select(key => JsonConvert.DeserializeObject<LogStorageFileInfo>(blockBlob.Metadata[key]));
        }

        #endregion
    }
}
