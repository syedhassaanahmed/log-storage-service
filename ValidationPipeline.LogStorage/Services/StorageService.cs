using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ValidationPipeline.LogStorage.Services
{
    public class StorageService : IStorageService
    {
        private const string ContainerName = "validationpipeline";
        private const string ZipContentType = "application/zip";

        private const string FileNamesCountMetaDataKey = "FileNamesCount";
        private const string FileNamesMetaDataKeyPrefix = "File";

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
            IList<string> innerFileNames)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            if (innerFileNames == null || !innerFileNames.Any())
                throw new ArgumentNullException(nameof(innerFileNames));

            var blockBlob = _blobContainer.GetBlockBlobReference(archiveFileName);
            blockBlob.Properties.ContentType = ZipContentType;

            StoreInnerFileNamesInMetaData(innerFileNames, blockBlob);

            await blockBlob.UploadFromStreamAsync(archiveStream);
        }

        public async Task<bool> ExistsAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            return await blob.ExistsAsync();
        }

        public async Task<IEnumerable<string>> GetInnerFileNamesAsync(string archiveFileName)
        {
            if(string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            var blob = _blobContainer.GetBlobReference(archiveFileName);
            await blob.FetchAttributesAsync();
            return RetrieveInnerFileNamesFromMetaData(blob);
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

        private static void StoreInnerFileNamesInMetaData(IList<string> innerFileNames, CloudBlob blockBlob)
        {
            blockBlob.Metadata.Add(FileNamesCountMetaDataKey, innerFileNames.Count.ToString());
            for (var i = 0; i < innerFileNames.Count; i++)
            {
                // MetaData Key Name follows C# identifiers convention
                // Hence we can't store file names in Key
                blockBlob.Metadata.Add($"{FileNamesMetaDataKeyPrefix}{i}", innerFileNames[i]);
            }
        }

        private static IEnumerable<string> RetrieveInnerFileNamesFromMetaData(CloudBlob blockBlob)
        {
            if (blockBlob.Metadata.Count == 0)
                return Enumerable.Empty<string>();

            var innerFileNamesCount = Convert.ToInt32(blockBlob.Metadata[FileNamesCountMetaDataKey]);

            var innerFileNames = new List<string>();
            for (var i = 0; i < innerFileNamesCount; i++)
            {
                var innerFileName = blockBlob.Metadata[$"{FileNamesMetaDataKeyPrefix}{i}"];
                innerFileNames.Add(innerFileName);
            }

            return innerFileNames;
        }

        #endregion
    }
}
