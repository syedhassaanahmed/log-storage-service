namespace ValidationPipeline.LogStorage.Models
{
    public class BasicAuthenticationOptions
    {
        public string TestUsername { get; set; } = "Jon";
        public string TestPassword { get; set; } = "Snow";
        public string[] ExcludePaths { get; set; } = { "/swagger/v1/swagger.json" };
    }
}