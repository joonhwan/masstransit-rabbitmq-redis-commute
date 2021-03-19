using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo04.ErrorHandling
{
    public class HostedServiceConsume : IHostedService
    {
        private readonly ILogger<HostedServiceConsume> _logger;
        private readonly IBusControl _bus;

        public HostedServiceConsume(ILogger<HostedServiceConsume> logger, IBusControl bus)
        {
            _logger = logger;
            _bus = bus;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consumer 서비스 시작합니다");
            var handle = await _bus.StartAsync(cancellationToken);
            _logger.LogInformation("Consumer 서비스 시작되었습니다");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consumer 서비스 종료합니다");
            await _bus.StopAsync(cancellationToken);
            _logger.LogInformation("Consumer 서비스 종료되었습니다");
        }
    }
}