using System;
using System.Threading;
using System.Threading.Tasks;
using HelloQuartzWorker.Model;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;

namespace HelloQuartzWorker
{
    public class MyService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly JobMetaData _jobMetaData;
        private readonly IJobFactory _jobFactory;
        private IScheduler _scheduler;

        public MyService(ISchedulerFactory schedulerFactory, JobMetaData jobMetaData, IJobFactory jobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _jobMetaData = jobMetaData;
            _jobFactory = jobFactory;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Scheduler 시작. 
            _scheduler = await _schedulerFactory.GetScheduler();
            _scheduler.JobFactory = _jobFactory;
            
            // Job 생성
            IJobDetail jobDetail = CreateJob(_jobMetaData);

            // Trigger 생성
            ITrigger trigger = CreateTrigger(_jobMetaData);

            // Schedule Job 
            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
            
            // Start the Scheduler
            await _scheduler.Start(cancellationToken);
        }

        private ITrigger CreateTrigger(JobMetaData jobMetaData)
        {
            return TriggerBuilder.Create()
                    .WithIdentity(jobMetaData.JobId.ToString("D"))
                    .WithCronSchedule(jobMetaData.CronExpression)
                    .WithDescription(jobMetaData.JobName)
                    .Build()
                ;
        }

        private IJobDetail CreateJob(JobMetaData jobMetaData)
        {
            return JobBuilder.Create(jobMetaData.JobType)
                .WithIdentity(jobMetaData.JobId.ToString("D"))
                .WithDescription(jobMetaData.JobName)
                .Build();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);
        }
    }
}