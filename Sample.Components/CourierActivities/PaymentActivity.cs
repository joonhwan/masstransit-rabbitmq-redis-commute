using System;
using System.Threading.Tasks;
using MassTransit.Courier;
using Microsoft.Extensions.Logging;

namespace Sample.Components.CourierActivities
{
    public class PaymentActivity : IActivity<PaymentArguments, PaymentLog>
    {
        private readonly ILogger<PaymentActivity> _logger;

        public PaymentActivity(ILogger<PaymentActivity> logger)
        {
            _logger = logger;
            Console.WriteLine("PaymentActivity 생성 : logger = {0}", _logger);
        }
        public async Task<ExecutionResult> Execute(ExecuteContext<PaymentArguments> context)
        {
            var cardNumber = context.Arguments.CardNumber;
            _logger.LogInformation("{CardNumber} 에 대하여 결재가 진행중입니다", cardNumber);
            
            if (string.IsNullOrEmpty(cardNumber))
            {
                throw new ArgumentNullException(nameof(cardNumber));
            }

            await Task.Delay(5000); // allocation 해제가 바로 일어나지는 않게...
            
            if (cardNumber.StartsWith("5999"))
            {
                _logger.LogError("5999 로 시작하는 CardNumber 는 사용불가함", cardNumber);
                throw new InvalidOperationException($"5999 로 시작하는 CardNumber 는 사용불가함");
            }
            
            await Task.Delay(2000);
            
            _logger.LogInformation("{CardNumber} 에 대하여 결재가 완료되었습니다", cardNumber);

            return context.Completed<PaymentLog>(new
            {
                AuthorizationCode = $"{context.Arguments.CardNumber}-OK",
                CardNumber = cardNumber
            });
        }

        public async Task<CompensationResult> Compensate(CompensateContext<PaymentLog> context)
        {
            var cardNumber = context.Log.CardNumber;
            _logger.LogWarning("{CardNumber} 에 대하여 결재취소가 시작됨", cardNumber);

            await Task.Delay(1000);
            
            _logger.LogWarning("{CardNumber} 에 대하여 결재취소가 완료됨", cardNumber);
            
            return context.Compensated();
        }
    }

    public interface PaymentArguments
    {
         Guid OrderId { get; }
         decimal Amount { get; }
         string CardNumber { get; }
    }

    public interface PaymentLog
    {
        string CardNumber { get; }
        string AuthorizationCode { get; }
    }
}