namespace Paperless.Services.Configurations
{
    public class GenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gemini-2.0-flash";
        public string ApiUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}




