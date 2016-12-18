using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.FileProviders
{
    public class LogStorageFileProvider : IFileProvider
    {
        private readonly IStorageService _storageService;

        public LogStorageFileProvider(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var pathSplits = subpath.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (pathSplits.Length != 2)
                return new NotFoundFileInfo(subpath);

            var archiveFileName = pathSplits[0];
            var innerFileName = pathSplits[1];

            var exists = _storageService.ExistsAsync(archiveFileName).Result;
            if (!exists)
                return new NotFoundFileInfo(archiveFileName);

            var metaData = _storageService.GetInnerFileMetaDataAsync(archiveFileName, 
                innerFileName).Result;

            if(metaData == null)
                return new NotFoundFileInfo(innerFileName);

            return new LogStorageFileInfo(async () => 
                await ExtractInnerFileAsync(archiveFileName, metaData))
            {
                Name = metaData.Name,
                Length = metaData.Length,
                LastModified = metaData.LastModified
            };
        }

        private async Task<Stream> ExtractInnerFileAsync(string archiveFileName, MetaData metaData)
        {
            var archiveStream = await _storageService.DownloadAsync(archiveFileName);

            using (var archiveService = new ArchiveService())
            {
                archiveService.Initialize(archiveStream);
                return archiveService.ExtractInnerFile(metaData.Name);
            }
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }
}
