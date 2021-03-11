using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Sample.Service
{
    internal class MassTransitConsoleHostedService : IHostedService
    {
        private IBusControl _bus;

        public MassTransitConsoleHostedService(IBusControl bus)
        {
            _bus = bus;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 왜 ConfigureAwait(false) 가 필요할까. 
            await _bus.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _bus.StopAsync(cancellationToken);
        }
    }
}