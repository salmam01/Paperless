namespace Paperless.BL.Configurations
{
    public class RabbitMQConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string User {  get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
    }
}
