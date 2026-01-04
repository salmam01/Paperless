using Microsoft.Extensions.Options;
using Paperless.BL.Configurations;
using RabbitMQ.Client;

namespace Paperless.BL.Services.Messaging
{
    public class MQConnectionFactory
    {
        private readonly RabbitMQConfig _config;
        public readonly ConnectionFactory ConnectionFactory;
        public readonly string ExchangeName;

        public MQConnectionFactory(IOptions<RabbitMQConfig> config)
        {
            _config = config.Value;

            ConnectionFactory = new ConnectionFactory()
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.User,
                Password = _config.Password
            };

            ExchangeName = _config.ExchangeName;
        }
    }
}
