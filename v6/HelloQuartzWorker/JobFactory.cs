using System;
using Quartz;
using Quartz.Spi;

namespace HelloQuartzWorker
{
    // DI 와 Quartz의 Factory 를 이어주는 역할.
    public class JobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            // mostly focsuing...this

            var jobDetail = bundle.JobDetail;
            return (IJob) _serviceProvider.GetService(jobDetail.JobType);
        }

        public void ReturnJob(IJob job)
        {
            throw new System.NotImplementedException();
        }
    }
}