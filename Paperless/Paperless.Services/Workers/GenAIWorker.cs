using Paperless.Services.Services.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Workers
{
    public class GenAIWorker : BackgroundService
    {
        private readonly ILogger<GenAIWorker> _logger;
        private readonly MQListener _messageQueueService;

        public GenAIWorker(
            ILogger<GenAIWorker> logger,
            MQListener messageQueueService
        ) {
            _logger = logger;
            _messageQueueService = messageQueueService;
        }

        //  Call GenAI endpoint for summary here
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
