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

            var notes = context.Message.Notes;
            if (notes.HasValue)
            {
                // 와우. 어디에선가 notes의 내용을 따로 가져온다(Message 자체에는 포함되지 않고!!!)
                var notesValue = await notes.Value;
                _logger.LogWarning("😁😁😁  와우 note 값이 있네요. : {Notes}", notesValue);
            }

            // context를 통해, 어떤 Message를  Consume하는 도중에 또 다른 Message를 Publish 할 수 있다. 
            await context.Publish<OrderSubmitted>(new
            {
                OrderId = context.Message.OrderId,
                Timestamp = context.Message.Timestamp,
                CustomerNumber = context.Message.CustomerNumber,
                PaymentCardNumber = context.Message.PaymentCardNumber,
                // Notes = new
                // {
                //     Value = default(Task),
                //     Address = default(Uri),
                //     HasValue = default(Boolean)
                // }
                Notes = context.Message.Notes // 사실, 이렇게 한다고 해서, 데이터 바이트 수 만큼이 Relay 되는것 아니다. --> MessageData<T> 의 특징.(물론 Threshold 보다 작은 바이트라면, Relay되겠지만...
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