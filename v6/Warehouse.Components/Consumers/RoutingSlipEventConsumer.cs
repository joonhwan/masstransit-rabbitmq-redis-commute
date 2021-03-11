using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.Extensions.Logging;

namespace Warehouse.Components.Consumers
{
    // Courier가 실행하는 모든 RoutingSlip 관련 처리 에 대한 모니터링을 아래처럼 할 수 있다.
    // 어떤 작업인지는 TackingNumber, Variables, ActivityName 등등을  가지고 알 수 있겠지. 
    //
    // Updated:  RoutingSlipCompleted 를 Batch 처리 쪽에서만 하도록 수정함.
    public class RoutingSlipEventConsumer : //IConsumer<RoutingSlipCompleted>,
                                            IConsumer<RoutingSlipActivityCompleted>,
                                            IConsumer<RoutingSlipFaulted>
    {
        private readonly ILogger<RoutingSlipEventConsumer> _logger;

        public RoutingSlipEventConsumer(ILogger<RoutingSlipEventConsumer> logger)
        {
            _logger = logger;
        }

        // public Task Consume(ConsumeContext<RoutingSlipCompleted> context)
        // {
        //     if (_logger.IsEnabled(LogLevel.Information))
        //     {
        //         //@ 오오. Log가 이제 Structured Logging이 되네.
        //         _logger.LogInformation("Routing Slip 이 완료되었습니다. : {TrackingNumber}", context.Message.TrackingNumber);
        //     }
        //
        //     return Task.CompletedTask;
        // }

        public Task Consume(ConsumeContext<RoutingSlipActivityCompleted> context)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                //@ 오오. Log가 이제 Structured Logging이 되네.
                _logger.LogInformation("Routing Slip Activity( {ActivityName} )가 완료되었습니다. : {TrackingNumber} ", context.Message.ActivityName, context.Message.TrackingNumber);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<RoutingSlipFaulted> context)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                //@ 오오. Log가 이제 Structured Logging이 되네.
                _logger.LogInformation("Routing Slip 가 실패했습니다. : {TrackingNumber} {ExceptionInfo}", context.Message.TrackingNumber, context.Message.ActivityExceptions.FirstOrDefault());
            }

            return Task.CompletedTask;
        }
    }
}