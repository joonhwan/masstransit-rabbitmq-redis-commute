using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;

namespace CommuteSystem.Consumers
{
    public class AuditOrderConsumer : IConsumer<AuditOrder>
    {
        private readonly ILogger<AuditOrderConsumer> _logger;

        public AuditOrderConsumer(ILogger<AuditOrderConsumer> logger)
        {
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<AuditOrder> context)
        {
            var message = context.Message;
            
            _logger.LogInformation("AuditOrder 처리시작 : {OrderId}", message.OrderId);
            await Task.Delay(1000);
            _logger.LogInformation("AuditOrder 처리완료 : {OrderId}", message.OrderId);

        }
    }

    public class AuditOrderConsumerDefinition : ConsumerDefinition<AuditOrderConsumer>
    {
        public AuditOrderConsumerDefinition()
        {
            EndpointName = "audit.service";

            // ConsumerDefinition 클래스에서 Concurrency 설정하는 방법
            ConcurrentMessageLimit = 10; 
        }
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<AuditOrderConsumer> consumerConfigurator)
        {
            // ConsumerDefinition 클래스에서 Prefetch 설정하는 방법
            // endpointConfigurator.PrefetchCount = 10;
        }
    }
}