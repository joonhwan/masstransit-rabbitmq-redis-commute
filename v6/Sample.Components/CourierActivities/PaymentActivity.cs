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
            var paymentCardNumber = context.Arguments.PaymentCardNumber;
            _logger.LogInformation("{PaymentCardNumber} 에 대하여 결재가 진행중입니다", paymentCardNumber);
            
            if (string.IsNullOrEmpty(paymentCardNumber))
            {
                throw new ArgumentNullException(nameof(paymentCardNumber));
            }

            await Task.Delay(5000); // allocation 해제가 바로 일어나지는 않게...
            
            if (paymentCardNumber.StartsWith("5999"))
            {
                _logger.LogError("5999 로 시작하는 PaymentCardNumber 는 사용불가함", paymentCardNumber);
                throw new InvalidOperationException($"5999 로 시작하는 PaymentCardNumber 는 사용불가함");
            }
            
            await Task.Delay(2000);
            
            _logger.LogInformation("{PaymentCardNumber} 에 대하여 결재가 완료되었습니다", paymentCardNumber);

            return context.Completed<PaymentLog>(new
            {
                AuthorizationCode = $"{context.Arguments.PaymentCardNumber}-OK",
                PaymentCardNumber = paymentCardNumber
            });
        }

        public async Task<CompensationResult> Compensate(CompensateContext<PaymentLog> context)
        {
            var paymentCardNumber = context.Log.PaymentCardNumber;
            _logger.LogWarning("{PaymentCardNumber} 에 대하여 결재취소가 시작됨", paymentCardNumber);

            await Task.Delay(1000);
            
            _logger.LogWarning("{PaymentCardNumber} 에 대하여 결재취소가 완료됨", paymentCardNumber);
            
            return context.Compensated();
        }
    }

    public interface PaymentArguments
    {
         Guid OrderId { get; }
         decimal Amount { get; }
         string PaymentCardNumber { get; }
    }

    public interface PaymentLog
    {
        string PaymentCardNumber { get; }
        string AuthorizationCode { get; }
    }
}