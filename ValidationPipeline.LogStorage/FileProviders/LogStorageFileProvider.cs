using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.FileProviders
{
    public class LogStorageFileProvider : IFileProvider
    {
        private readonly IArchiveService _archiveService;
        private readonly IStorageService _storageService;

        public LogStorageFileProvider()
        {
            //_archiveService = archiveService;
            //_storageService = storageService;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return new LogStorageFileInfo();
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
