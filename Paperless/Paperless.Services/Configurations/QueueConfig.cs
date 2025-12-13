namespace Paperless.Services.Configurations
{
    public class QueueConfig
    {
        public string QueueName { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
    }
}
