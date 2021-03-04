using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HelloQuartzWorker.Job
{
    public class NotificationJob : IJob
    {
        private readonly ILogger<NotificationJob> _logger;

        public NotificationJob(ILogger<NotificationJob> logger)
        {
            _logger = logger;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Notification 이 시작되었습니다. {DateTime.Now}, job type = {context.JobDetail.JobType}");
            await Task.Delay(3000);
            _logger.LogInformation($"Notification 이 완료되었습니다. {DateTime.Now}, job type = {context.JobDetail.JobType}");
        }
    }
}