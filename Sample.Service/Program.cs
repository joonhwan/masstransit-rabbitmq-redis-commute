using System;
using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Components;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace Sample.Service
{
    static class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);
            var isWindowsService = !(Debugger.IsAttached || args.Contains("--console"));
            if (isWindowsService)
            {
                await hostBuilder.UseWindowsService().Build().RunAsync();
            }
            else
            {
                await hostBuilder.RunConsoleAsync();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
                    services.AddMassTransit(configurator =>
                    {
                        // SubmitOrderConsumer가 있는 assembly내의 모든 consumer를 추가. 
                        configurator.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                        
                        // 일종의 Mediator 역할을 하는 Bus 를 추가.
                        configurator.AddBus(ConfigureBus);
                    });
                    services.AddHostedService<MassTransitConsoleHostedService>();
                });

        private static IBusControl ConfigureBus(IServiceProvider serviceProvider)
        {
            return Bus.Factory.CreateUsingRabbitMq(configurator =>
            {
                configurator.Host("rabbitmq://admin:mirero@localhost:5672");
                configurator.ConfigureEndpoints(serviceProvider, KebabCaseEndpointNameFormatter.Instance);
            });
        }
    }
}