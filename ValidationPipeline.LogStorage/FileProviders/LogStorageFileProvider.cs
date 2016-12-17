using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.FileProviders
{
    public class LogStorageFileProvider : IFileProvider
    {
        private readonly IArchiveService _archiveService;
        private readonly IStorageService _storageService;

        public LogStorageFileProvider(IArchiveService archiveService, 
            IStorageService storageService)
        {
            _archiveService = archiveService;
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
            {
                var archiveStream = await _storageService.DownloadAsync(archiveFileName);
                return _archiveService.ExtractInnerFile(archiveStream, metaData.Name);
            })
            {
                Name = metaData.Name,
                Length = metaData.Length,
                LastModified = metaData.LastModified
            };
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
