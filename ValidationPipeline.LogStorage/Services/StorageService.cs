using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NuGet.Packaging;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Services
{
    public class StorageService : IStorageService
    {
        private const string ContainerName = "validationpipeline";

        private readonly IOptionsSnapshot<BlobStorageOptions> _options;
        private readonly CloudBlobContainer _blobContainer;

        public StorageService(IOptionsSnapshot<BlobStorageOptions> options)
        {
            _options = options;

            var storageAccount = CloudStorageAccount.Parse(options.Value.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            _blobContainer = blobClient.GetContainerReference(ContainerName);

            _blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, 
                GetLatestRequestOptions(), new OperationContext()).Wait();
        }

        private BlobRequestOptions GetLatestRequestOptions()
        {
            return new BlobRequestOptions
            {
                SingleBlobUploadThresholdInBytes = _options.Value.SingleBlobUploadThresholdInBytes,
                ParallelOperationThreadCount = _options.Value.ParallelOperationThreadCount
            };
        }

        #region IStorageService Implementation

        public async Task UploadAsync(string archiveFileName, Stream archiveStream,
            IDictionary<string, MetaData> metaData)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            if (metaData == null || !metaData.Any())
                throw new ArgumentNullException(nameof(metaData));

            var blockBlob = _blobContainer.GetBlockBlobReference(archiveFileName);

            blockBlob.Metadata.AddRange(metaData.ToDictionary(
                entry => entry.Key, entry => JsonConvert.SerializeObject(entry.Value)));

            archiveStream.Position = 0;

            await blockBlob.UploadFromStreamAsync(archiveStream, 
                AccessCondition.GenerateEmptyCondition(),
                GetLatestRequestOptions(), new OperationContext());
        }

        public async Task<bool> ExistsAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            return await blob.ExistsAsync(GetLatestRequestOptions(), new OperationContext());
        }

        public async Task<IDictionary<string, MetaData>> GetMetaDataAsync(string archiveFileName)
        {
            if(string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            await blob.FetchAttributesAsync(AccessCondition.GenerateEmptyCondition(), 
                GetLatestRequestOptions(), new OperationContext());

            return blob.Metadata.ToDictionary(entry => entry.Key,
                entry => JsonConvert.DeserializeObject<MetaData>(entry.Value));
        }

        public async Task<MetaData> GetInnerFileMetaDataAsync(string archiveFileName,
            string innerFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            if (string.IsNullOrWhiteSpace(innerFileName))
                throw new ArgumentNullException(nameof(innerFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            await blob.FetchAttributesAsync(AccessCondition.GenerateEmptyCondition(),
                GetLatestRequestOptions(), new OperationContext());

            return blob.Metadata.ContainsKey(innerFileName) ? 
                JsonConvert.DeserializeObject<MetaData>(blob.Metadata[innerFileName]) : null;
        }

        public async Task<Stream> DownloadAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blockBlob = _blobContainer.GetBlockBlobReference(archiveFileName);
            var memoryStream = new MemoryStream();

            await blockBlob.DownloadToStreamAsync(memoryStream, AccessCondition.GenerateEmptyCondition(),
                GetLatestRequestOptions(), new OperationContext());

            return memoryStream;
        }

        #endregion
    }
}
