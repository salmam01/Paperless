using Quartz;

namespace Paperless.Batch
{
    public class AccessDataJob : IJob
    {
        private readonly ILogger<AccessDataJob> _logger;

        public AccessDataJob(ILogger<AccessDataJob> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
