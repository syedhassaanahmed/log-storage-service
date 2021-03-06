﻿namespace ValidationPipeline.LogStorage.Models
{
    public class BlobStorageOptions
    {
        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        public long SingleBlobUploadThresholdInBytes { get; set; } = 32*1024*1024;
        public int ParallelOperationThreadCount { get; set; } = 1;
    }
}
