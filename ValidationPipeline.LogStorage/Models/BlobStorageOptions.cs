namespace ValidationPipeline.LogStorage.Models
{
    public class BlobStorageOptions
    {
        public string ConnectionString { get; set; }
        public long SingleBlobUploadThresholdInBytes { get; set; } = 1024*1024;
        public int ParallelOperationThreadCount { get; set; } = 1;
    }
}
