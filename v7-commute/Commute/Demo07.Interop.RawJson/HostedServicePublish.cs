using System;
using System.Threading;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using Demo07.Interop.RawJson.Messages;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo07.Interop.RawJson
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

            var random = new Random();
            var x = 0;
            var y = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                var dx = random.Next(-5, 5);
                var dy = random.Next(-5, 5);
                x += dx;
                y += dy;
                _logger.LogInformation("내부장치가 이동했습니다. ({x},{y})", x, y);
                await _busControl.Publish<UpdateLocation>(new 
                    {
                        DeviceId = "InternalDevice-01",
                        X = x,
                        Y = y
                    },
                    stoppingToken);

                _logger.LogInformation("대기중...");
                await Task.Delay(5000, stoppingToken);
            }

            _logger.LogInformation("명령전송을 종료합니다.");
        }
    }

    public class AuditOrderCommand : AuditOrder
    {
        public Guid OrderId { get; set; }
    }
}