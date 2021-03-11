using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Courier.Contracts;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;

namespace Sample.Components.BatchConsumers
{
    // RoutingSlipCompleted 메시지를 Batch 로 처리 
    //   - 정해진 Threshold 이상의 메시지가 쌓였을때, 한번에 처리.
    public class RoutingSlipBatchEventConsumer : IConsumer<Batch<RoutingSlipCompleted>>
    {
        private readonly ILogger<RoutingSlipBatchEventConsumer> _logger;

        public RoutingSlipBatchEventConsumer(ILogger<RoutingSlipBatchEventConsumer> logger)
        {
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<Batch<RoutingSlipCompleted>> context)
        {
            _logger.LogInformation("Routing Slip 작업(들)이 완료되었음 : {TrackingNumbers}",
                string.Join(", ",context.Message.Select(x => x.Message.TrackingNumber)));
            
            return Task.CompletedTask;
        }
    }
    
    // @new-masstransit-batch-consumer-setup
    // @legacy-masstransit-batch-consumer-setup 에서 했던 거 보다 더 나은 방법으로 보임.
    // -->https://masstransit-project.com/advanced/batching.html#batching 참고
    // class OrderAuditConsumerDefinition : ConsumerDefinition<RoutingSlipBatchEventConsumer>
    // {
    //     public OrderAuditConsumerDefinition()
    //     {
    //         Endpoint(x => x.PrefetchCount = 10);
    //     }
    //
    //     protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
    //         IConsumerConfigurator<RoutingSlipBatchEventConsumer> consumerConfigurator)
    //     {
    //         consumerConfigurator.Options<BatchOptions>(options => options
    //             .SetMessageLimit(10)
    //             .SetTimeLimit(TimeSpan.FromSeconds(5))
    //         );
    //     }
    // }
    
}