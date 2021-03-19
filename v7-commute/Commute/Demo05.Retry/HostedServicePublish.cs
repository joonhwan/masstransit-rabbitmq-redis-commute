using System;
using System.Threading;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo05.Retry
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
            _logger.LogInformation("명령전송합니다.");
            
            await _busControl.Publish<SubmitClaim>(new SubmitClaimCommand
                {
                    ClaimContents = "어려운 부탁.",
                    CustomerId = _customerId,
                    OrderId = _orderId,
                    DegreeOfHardness = 3, // 아마 3번은 재시도 해야 할 거임
                },
                stoppingToken);
            
            _logger.LogInformation("명령전송을 종료합니다.");
        }
    }

    public class SubmitClaimCommand : SubmitClaim
    {
        public Guid CustomerId { get; set; }
        public Guid OrderId { get; set; }
        public string ClaimContents { get; set; }
        public int DegreeOfHardness { get; set; }
    }
}