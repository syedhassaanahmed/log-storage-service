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

        private readonly CloudBlobClient _blobClient;
        private readonly CloudBlobContainer _blobContainer;

        public StorageService(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(ContainerName);
        }

        public async Task<IEnumerable<string>> GetInnerFileNames(string archiveFileName)
        {
            if(string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            await _blobContainer.CreateIfNotExistsAsync();
            throw new NotImplementedException();
        }

        public async Task<bool> UploadAsync(string archiveFileName, Stream archiveStream, 
            IEnumerable<string> innerFileNames)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            if (innerFileNames == null || !innerFileNames.Any())
                throw new ArgumentNullException(nameof(innerFileNames));

            await _blobContainer.CreateIfNotExistsAsync();
            throw new NotImplementedException();
        }

        public async Task<Stream> DownloadAsync(string archiveFileName)
        {
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentNullException(nameof(archiveFileName));

            await _blobContainer.CreateIfNotExistsAsync();
            throw new NotImplementedException();
        }
    }
}
