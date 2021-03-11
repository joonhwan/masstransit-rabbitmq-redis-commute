using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sample.Contracts;

namespace Sample.Components.Consumers
{
    // 어떤 메시지 T 에 대해 처리중 예외발생하면, Fault<T> 가 Publish 된다. 
    // T 형 메시지에 FaultedAddress 또는 ResponseAddress가 있으면, 그 주소로 Send ..  
    // 이런게 없으면, 그냥 Publish 된다. (RabbitMQ의 `MassTransit:Fault--Sample.Contracts:FulfillOrder--` 를 타고...)
    // Fault<T> 메시지는 Error Queue 로 가는 메시지는 아니다. 
    // 
    public class FaultConsumer : IConsumer<Fault<FulfillOrder>>
    {
        private readonly ILogger<FaultConsumer> _logger;

        public FaultConsumer(ILogger<FaultConsumer> logger)
        {
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<Fault<FulfillOrder>> context)
        {
            var faultAddress = context.FaultAddress;
            var sourceAddress = context.SourceAddress;
            var messageTypes = string.Join(", ", context.Message.FaultMessageTypes);
            var exceptionMessages = string.Join(", ", context.Message.Exceptions.Select(info => info.Message));

            _logger.LogWarning("!!!!!!! 아아아. 오류가 있었네요. 확인해보세요.." +
                               "오류난 메시지 정보 : 유형=[{MessageTypes}], 예외정보=[{ExceptionMessage}]." +
                               "SourceAddress=[{SourceAddress}], FaultAddress = [{FaultAddress}],  ",
                messageTypes,
                exceptionMessages,
                sourceAddress,
                faultAddress
            );

            return Task.CompletedTask;
        }
    }
}