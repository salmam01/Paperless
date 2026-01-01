using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;

namespace Paperless.Services.Services.Messaging.Base
{
    public class MQConnectionFactory
    {
        private readonly RabbitMQConfig _config;
        public readonly ConnectionFactory ConnectionFactory;
        public readonly string ExchangeName;

        public MQConnectionFactory(IOptions<RabbitMQConfig> rabbitMqConfig)
        {
            _config = rabbitMqConfig.Value;

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
