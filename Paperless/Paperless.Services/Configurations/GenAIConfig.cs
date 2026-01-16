namespace Paperless.Services.Configurations
{
    public class GenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}




