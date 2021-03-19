using System;
using System.Threading;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PublishDemo
{
    public class HostedServicePublish : BackgroundService
    {
        private readonly ILogger<HostedServicePublish> _logger;
        private readonly IBusControl _busControl;
        private readonly Guid _customerId = Guid.NewGuid();
        private readonly Guid _orderId = Guid.NewGuid();

        public HostedServicePublish(ILogger<HostedServicePublish> logger, IBusControl busControl)
        {
            _logger = logger;
            _busControl = busControl;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("명령전송합니다.");
                await _busControl.Publish<SubmitClaim>(new SubmitClaimCommand
                    {
                        ClaimContents = $"{DateTime.UtcNow} 에 발생한 고객 클레임",
                        CustomerId = _customerId,
                        OrderId = _orderId,
                    },
                    stoppingToken);

                _logger.LogInformation("잠시쉬는중입니다.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("명령전송을 종료합니다.");
        }
    }

    public class SubmitClaimCommand : SubmitClaim
    {
        public Guid CustomerId { get; set; }
        public Guid OrderId { get; set; }
        public string ClaimContents { get; set; }
        public int DegreeOfHardness { get; set; } = 0;
    }
}