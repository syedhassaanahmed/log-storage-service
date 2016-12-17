using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace ValidationPipeline.LogStorage.FileProviders
{
    public class LogStorageFileInfo : IFileInfo
    {
        private readonly Func<Task<Stream>> _streamFunc;

        public LogStorageFileInfo(Func<Task<Stream>> streamFunc)
        {
            _streamFunc = streamFunc;
        }

        public Stream CreateReadStream()
        {
            return _streamFunc().Result;
        }

        public bool Exists => true;
        public long Length { get; set; }
        public string PhysicalPath => string.Empty;
        public string Name { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public bool IsDirectory => false;
    }
}
