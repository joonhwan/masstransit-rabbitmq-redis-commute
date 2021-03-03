using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sample.Contracts;

namespace Sample.Components.Consumers
{
    public class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        private readonly ILogger<SubmitOrderConsumer> _logger;

        public SubmitOrderConsumer(ILogger<SubmitOrderConsumer> logger)
        {
            _logger = logger;
        }
        
        public SubmitOrderConsumer()
        {
            _logger = new NullLogger<SubmitOrderConsumer>();
        }
        
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            var shouldRespond = context.ResponseAddress != null; // 응답을 받는 넘이 자신의 주소를 준다. 안주면, 그냥 publish 한거. 
            
            if (context.Message.CustomerNumber.Contains("TEST"))
            {
                _logger.LogInformation("테스트유저는 주문을 못해요.😒");
                
                // 아래 처럼 throw 하면 '*_error' 라는 이름의 queue에 수신된 메시지가 들어간다.
                //throw new InvalidOperationException("테스트유저는 주문을 못해요.😒");
                // --> 그럼, 메시지를 IRequestClient.GetResponse() 호출한 아이는 어떻게 되나?

                if (shouldRespond)
                {
                    // Consumer 는 1개 이상의 Message를 반환할 수 있다.
                    // see @more-than-one-response-message
                    await context.RespondAsync<OrderSubmissionRejected>(new
                    {
                        context.Message.OrderId,
                        TimeStamp = InVar.Timestamp,
                        CustomerNumber = context.Message.CustomerNumber
                    });
                }

                return;
            }

            // context를 통해, 어떤 Message를  Consume하는 도중에 또 다른 Message를 Publish 할 수 있다. 
            await context.Publish<OrderSubmitted>(new
            {
                OrderId = context.Message.OrderId,
                Timestamp = context.Message.Timestamp,
                CustomerNumber = context.Message.CustomerNumber
            });
            
            // 아래 주석 처리된 부분은... 희한하게도 수신된 메시지에서 값이 자동 복사된다고 한다.
            if (shouldRespond)
            {
                await context.RespondAsync<OrderSubmissionAccepted>(new
                {
                    context.Message.OrderId,
                    TimeStamp = InVar.Timestamp,
                    CustomerNumber = context.Message.CustomerNumber
                });
            }
        }
    }
}