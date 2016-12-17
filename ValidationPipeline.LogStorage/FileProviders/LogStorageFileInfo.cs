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
        public long Length => 10;
        public string PhysicalPath => string.Empty;
        public string Name => "somefile.txt";
        public DateTimeOffset LastModified => DateTimeOffset.Parse("1-1-2016");
        public bool IsDirectory => false;
    }
}
