using Paperless.Batch.Models;
using Quartz;

namespace Paperless.Batch
{
    [DisallowConcurrentExecution]
    public class AccessDataJob : IJob
    {
        private readonly ILogger<AccessDataJob> _logger;
        private readonly AccessDataBatchProcessor _processor;

        public AccessDataJob(
            ILogger<AccessDataJob> logger,
            AccessDataBatchProcessor processor
        ) {
            _logger = logger;
            _processor = processor;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting Batch Processing Job for Data Access Logs...");
            List<AccessEntryList> accessData = _processor.StartProcessing();

            throw new NotImplementedException();
        }
    }
}
