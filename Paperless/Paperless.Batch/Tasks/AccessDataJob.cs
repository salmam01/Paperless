using Paperless.Batch.Database;
using Paperless.Batch.Models;
using Quartz;

namespace Paperless.Batch.Tasks
{
    [DisallowConcurrentExecution]
    public class AccessDataJob : IJob
    {
        private readonly ILogger<AccessDataJob> _logger;
        private readonly AccessDataBatchProcessor _processor;
        private readonly IDocumentRepository _repository;

        public AccessDataJob(
            ILogger<AccessDataJob> logger,
            AccessDataBatchProcessor processor,
            IDocumentRepository repository
        ) {
            _logger = logger;
            _processor = processor;
            _repository = repository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting Batch Processing Job for Data Access Logs...");
            List<AccessEntryList> accessData = _processor.StartProcessing();

            if (accessData.Count == 0)
            {
                _logger.LogError("No Access Data to process.");
                return;
            }

            foreach (AccessEntryList accessEntryList in accessData) {

                if (!await _repository.UpdateDocumentsAsync(accessEntryList))
                {
                    _logger.LogError("Failed to update {Count} Access Data Entries with Date {Date} in Database.",
                        accessEntryList.AccessEntries.Count,
                        accessEntryList.AccessDate
                    );
                }
                _logger.LogInformation(
                    "Added {Count} Access Entries with Date {Date} to Database successfully.",
                    accessEntryList.AccessEntries.Count,
                    accessEntryList.AccessDate

                );
            }
        }
    }
}
