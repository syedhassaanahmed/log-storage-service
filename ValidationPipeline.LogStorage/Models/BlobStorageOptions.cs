namespace ValidationPipeline.LogStorage.Models
{
    public class BlobStorageOptions
    {
        public string ConnectionString { get; set; }
        public int ParallelOperationThreadCount { get; set; } = 1;
    }
}
