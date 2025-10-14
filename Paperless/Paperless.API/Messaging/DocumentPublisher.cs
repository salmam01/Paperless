using RabbitMQ.Client;

namespace Paperless.API.Messaging
{
    public class DocumentPublisher
    {
        //  Add to configuration file later
        private readonly IConnection _connection;
        private readonly int Port = 5672;
        private readonly string _queueName = "ocrQueue";
        private readonly ILogger<DocumentPublisher> _logger;

        public DocumentPublisher()
        {
            
        }

        public void PublishDocument()
        {
            try
            {
                
            } catch {
                /*
                using var connection = _connection.CreateConnection();
                using var channel = connection.CreateChannel();
                */
            }
        }
    }
}
