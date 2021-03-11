using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using LongRun.Contracts;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.JobService;
using Microsoft.Extensions.Logging;

namespace LongRun.Components
{
    public class DoItConsumer : IConsumer<DoIt>
    {
        private readonly ILogger<DoItConsumer> _logger;

        public DoItConsumer(ILogger<DoItConsumer> logger)
        {
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<DoIt> context)
        {
            _logger.LogInformation("@@@ DoIt 일반 Consumer가 작업 시작합니다. ");
            var message = context.Message;
            await Task.Delay(message.Duration);
            _logger.LogInformation("@@@ DoIt 일반 Consumer가 작업 완료했습니다. ");
        }
    }
    
    public class DoItJobConsumer : IJobConsumer<DoIt>
    {
        private readonly ILogger<DoItJobConsumer> _logger;

        public DoItJobConsumer(ILogger<DoItJobConsumer> logger)
        {
            _logger = logger;
        }
        
        public async Task Run(JobContext<DoIt> context)
        {
            var job = context.Job;
            
            _logger.LogInformation("@@@ DoIt Job Consumer가 작업 시작합니다 : Command = \"{Command}\"", job.Command);
            
            var duration = job.Duration;
            
            if (job.Command.ToLower().Contains("hard"))
            {
                var preprocessTime = TimeSpan.FromSeconds(10);
                _logger.LogWarning("@@@ DoIt Job Consumer 가 까다로운 작업을 검출했습니다. {PreprocessTime} 시간 동안 고민중...", preprocessTime);
                await Task.Delay(preprocessTime);

                if (context.RetryAttempt < 2)
                {
                    _logger.LogWarning("@@@ DoIt Job Consumer 는 어려운 명령(Command = \"{Command}\" 을 최소 3번은 해야되요." +
                                       " 현재 Retry횟수는 겨우 {Retry} 번 한겁니다.", job.Command, context.RetryAttempt);
                    throw new ApplicationException($"어려운 DoIt Job (Command = \"{job.Command}\") 실패(Retry={context.RetryAttempt})");
                }
            }
            await Task.Delay(duration, context.CancellationToken);
            
            _logger.LogInformation("@@@ DoIt Job Consumer가 작업 완료했습니다 : Command = \"{Command}\"", job.Command);
        }
    }

    public class DoItJobConsumerDefinition : ConsumerDefinition<DoItJobConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DoItJobConsumer> consumerConfigurator)
        {
            consumerConfigurator.Options<JobOptions<DoIt>>(options =>
            {
                options
                    .SetRetry(r =>
                    {
                        r.Interval(3, TimeSpan.FromSeconds(5));
                    })
                    .SetJobTimeout(TimeSpan.FromMinutes(10))
                    .SetConcurrentJobLimit(10)
                    ;
            });
        }
    }
}