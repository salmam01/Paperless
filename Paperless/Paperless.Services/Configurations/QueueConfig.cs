namespace Paperless.Services.Configurations
{
    public class QueueConfig
    {
        public string QueueName { get; set; } = string.Empty;
        public List<string> RoutingKeys { get; set; } = [];
        public int MaxRetries { get; set; } = 3;
    }
}
