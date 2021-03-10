using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.TestFramework;
using Microsoft.Extensions.Hosting;

namespace TrashLantis.Service
{
    public class MassTransitConsoleHostedService :
        IHostedService
    {
        readonly IBusControl _bus;

        public MassTransitConsoleHostedService(IBusControl bus)
        {
            _bus = bus;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var output = _bus.GetProbeResult().ToJsonString(); // Masstransit.TestFramework ;-)
            Console.WriteLine(output);
            
            await _bus.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _bus.StopAsync(cancellationToken);
        }
    }
}