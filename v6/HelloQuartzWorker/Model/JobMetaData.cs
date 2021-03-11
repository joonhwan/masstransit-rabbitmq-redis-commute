using System;

namespace HelloQuartzWorker.Model
{
    public class JobMetaData
    {
        public Guid JobId { get; set; }
        public Type JobType { get; }
        public string JobName { get; }
        public string CronExpression { get;  }

        public JobMetaData(Guid id, Type jobType, string jobName, string cronExpression)
        {
            JobId = id;
            JobType = jobType;
            JobName = jobName;
            CronExpression = cronExpression;
        }
    }
}