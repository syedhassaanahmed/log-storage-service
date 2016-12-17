using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace ValidationPipeline.LogStorage.FileProviders
{
    public class LogStorageFileInfo : IFileInfo
    {
        public Stream CreateReadStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        }

        public bool Exists => true;
        public long Length { get; set; }
        public string PhysicalPath => string.Empty;
        public string Name { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public bool IsDirectory => false;
    }
}
