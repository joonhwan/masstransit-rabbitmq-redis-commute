using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Courier.Contracts;
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
    
}