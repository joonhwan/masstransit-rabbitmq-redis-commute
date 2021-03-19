using System;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CommuteSystem.Consumers
{
    public class OrderSubmissionConsumer : IConsumer<SubmitOrder>
    {
        private readonly ILogger<SubmitOrder> _logger;

        public OrderSubmissionConsumer(ILogger<SubmitOrder> logger)
        {
            _logger = logger;
        }
        
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            var message = context.Message;
            
            _logger.LogInformation("SubmitOrder 처리시작 : {OrderId} (Amount = {Amount})", message.OrderId, message.Amount);
            
            await Task.Delay(1000);
            
            // @submit-order-error
            if (message.Amount <= 0)
            {
                throw new InvalidOrderAmount($"{message.Amount} 는 유효하지 않은 수량입니다");
            }
            _logger.LogInformation("SubmitOrder 처리완료 : {OrderId}", message.OrderId);
        }
    }

    public class InvalidOrderAmount : Exception
    {
        public InvalidOrderAmount(string message)
            : base(message)
        {
        }
    }
}