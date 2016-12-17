using System;

namespace ValidationPipeline.LogStorage.Models
{
    public class MetaData
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }
}
