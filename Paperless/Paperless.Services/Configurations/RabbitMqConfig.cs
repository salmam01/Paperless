namespace Paperless.Services.Configurations
{
    public class RabbitMqConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string OcrQueue { get; set; } = string.Empty;
        public string SummaryQueue { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
    }
}