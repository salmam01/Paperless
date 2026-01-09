using Microsoft.Extensions.Options;
using Paperless.Batch.Configuration;
using Quartz;

namespace Paperless.Batch
{
    public class AccessDataJobSetup : IConfigureOptions<QuartzOptions>
    {
        private readonly JobConfiguration _config;
        public AccessDataJobSetup(IOptions<JobConfiguration> config)
        {
            _config = config.Value;
        }

        public void Configure(QuartzOptions options)
        {
            var jobKey = JobKey.Create(nameof(AccessDataJob));
            options
                .AddJob<AccessDataJob>(jobBuilder => jobBuilder.WithIdentity(jobKey))
                .AddTrigger(trigger => 
                    trigger
                        .ForJob(jobKey)
                        .WithCronSchedule(_config.CronExpression));
        }
    }
}
