namespace Paperless.BL.Configurations
{
    public class RabbitMqConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string User {  get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
    }
}
