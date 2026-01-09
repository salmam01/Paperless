namespace Paperless.Batch
{
    public class BatchProcessingWorker : BackgroundService
    {
        private readonly ILogger<BatchProcessingWorker> _logger;

        public BatchProcessingWorker(ILogger<BatchProcessingWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
