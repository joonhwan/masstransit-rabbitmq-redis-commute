using System;
using System.Threading;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo04.ErrorHandling
{
    public class HostedServicePublish : BackgroundService
    {
        private readonly ILogger<HostedServicePublish> _logger;
        private readonly IBusControl _busControl;
        private readonly Guid _customerId = Guid.NewGuid();
        private readonly Guid _orderId = Guid.NewGuid();
        private readonly Guid _productId = Guid.NewGuid();

        public HostedServicePublish(ILogger<HostedServicePublish> logger, IBusControl busControl)
        {
            _logger = logger;
            _busControl = busControl;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 5번 요청중 1번은 오류가 날것임. @submit-order-error 코드 부분 참고
            int amount = 5;
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("명령전송합니다.");
                
                await _busControl.Publish<SubmitOrder>(new SubmitOrderCommand
                    {
                        CustomerId = _customerId,
                        OrderId = _orderId,
                        ProductId = _productId,
                        Amount = amount,
                    },
                    stoppingToken);

                amount -= 1;
                if (amount < 0)
                {
                    break;
                }
                
                _logger.LogInformation("잠시쉬는중입니다.");
                await Task.Delay(TimeSpan.FromSeconds(1.5), stoppingToken);
            }

            _logger.LogInformation("명령전송을 종료합니다.");
        }
    }

    public class SubmitOrderCommand : SubmitOrder
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Amount { get; set; }
        public Guid CustomerId { get; set; }
    }
}