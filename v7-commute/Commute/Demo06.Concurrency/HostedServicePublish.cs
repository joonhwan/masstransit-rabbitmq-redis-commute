using System;
using System.Threading;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo06.Concurrency
{
    public class HostedServicePublish : BackgroundService
    {
        private readonly ILogger<HostedServicePublish> _logger;
        private readonly IBusControl _busControl;
        private readonly Guid _orderId = Guid.NewGuid();
        
        public HostedServicePublish(ILogger<HostedServicePublish> logger, IBusControl busControl)
        {
            _logger = logger;
            _busControl = busControl;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("명령전송합니다.");
            
            await _busControl.Publish<AuditOrder>(new AuditOrderCommand
                {
                    OrderId = _orderId,
                },
                stoppingToken);
            
            _logger.LogInformation("명령전송을 종료합니다.");
        }
    }

    public class AuditOrderCommand : AuditOrder
    {
        public Guid OrderId { get; set; }
    }
}