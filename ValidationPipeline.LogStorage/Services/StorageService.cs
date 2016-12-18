using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private readonly CloudBlobClient _blobClient;

        public StorageService(IOptionsSnapshot<BlobStorageOptions> options)
        {
            _options = options;

            var storageAccount = CloudStorageAccount.Parse(options.Value.ConnectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
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

            var blobContainer = await CreateContainerIfNotExistsAsync();
            var blockBlob = blobContainer.GetBlockBlobReference(archiveFileName);

            blockBlob.Metadata.AddRange(metaData.ToDictionary(
                entry => entry.Key, entry => JsonConvert.SerializeObject(entry.Value)));

            blockBlob.Properties.ContentType = CommonConstants.ZipContentType;
            blockBlob.Properties.ContentMD5 = ComputeContentMd5(archiveStream);

            await blockBlob.UploadFromStreamAsync(archiveStream, 
                AccessCondition.GenerateEmptyCondition(),
                GetLatestRequestOptions(), new OperationContext());
        }

        public async Task<bool> ExistsAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blobContainer = await CreateContainerIfNotExistsAsync();
            var blob = blobContainer.GetBlobReference(archiveFileName);
            return await blob.ExistsAsync(GetLatestRequestOptions(), new OperationContext());
        }

        public async Task<IDictionary<string, MetaData>> GetMetaDataAsync(string archiveFileName)
        {
            if(string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blobContainer = await CreateContainerIfNotExistsAsync();
            var blob = blobContainer.GetBlobReference(archiveFileName);
            await FetchAttributesAsync(blob);

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

            var blobContainer = await CreateContainerIfNotExistsAsync();
            var blob = blobContainer.GetBlobReference(archiveFileName);
            await FetchAttributesAsync(blob);

            return blob.Metadata.ContainsKey(innerFileName) ? 
                JsonConvert.DeserializeObject<MetaData>(blob.Metadata[innerFileName]) : null;
        }

        public async Task<Stream> DownloadAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blobContainer = await CreateContainerIfNotExistsAsync();
            var blockBlob = blobContainer.GetBlockBlobReference(archiveFileName);
            var memoryStream = new MemoryStream();

            await blockBlob.DownloadToStreamAsync(memoryStream, AccessCondition.GenerateEmptyCondition(),
                GetLatestRequestOptions(), new OperationContext());

            if (ComputeContentMd5(memoryStream) != blockBlob.Properties.ContentMD5)
                throw new InvalidDataException($"{archiveFileName} is corrupt!");

            return memoryStream;
        }

        #endregion

        #region Helpers

        private async Task<CloudBlobContainer> CreateContainerIfNotExistsAsync()
        {
            var blobContainer = _blobClient.GetContainerReference(ContainerName);

            await blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off,
                GetLatestRequestOptions(), new OperationContext());

            return blobContainer;
        }

        private BlobRequestOptions GetLatestRequestOptions()
        {
            return new BlobRequestOptions
            {
                SingleBlobUploadThresholdInBytes = _options.Value.SingleBlobUploadThresholdInBytes,
                ParallelOperationThreadCount = _options.Value.ParallelOperationThreadCount
            };
        }

        private async Task FetchAttributesAsync(CloudBlob blob)
        {
            await blob.FetchAttributesAsync(AccessCondition.GenerateEmptyCondition(),
                GetLatestRequestOptions(), new OperationContext());
        }

        private static string ComputeContentMd5(Stream stream)
        {
            stream.Position = 0;

            using (var md5 = MD5.Create())
            {
                md5.Initialize();
                var bytes = md5.ComputeHash(stream);

                stream.Position = 0;
                return Convert.ToBase64String(bytes);
            }
        }

        #endregion
    }
}
