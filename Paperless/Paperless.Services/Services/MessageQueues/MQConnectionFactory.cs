using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Services.MessageQueues
{
    public class MQConnectionFactory
    {
        private readonly RabbitMqConfig _config;
        public readonly ConnectionFactory ConnectionFactory;

        public MQConnectionFactory(IOptions<RabbitMqConfig> rabbitMqConfig)
        {
            _config = rabbitMqConfig.Value;

            ConnectionFactory = new ConnectionFactory()
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.User,
                Password = _config.Password,
            };
        }
    }
}
