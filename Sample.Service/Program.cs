using System;
using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Components;
using System.Linq;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTransit.RedisIntegration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sample.Components.StateMachines;
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
                    //services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance); --> 이렇게 해도 되고. @2 처럼 해도 되고? 
                    services.AddMassTransit(configurator =>
                    {
                        configurator.SetKebabCaseEndpointNameFormatter(); // @2 --> @! 처럼 해도되고?! 이게 더 읽기 편하네.
                        // SubmitOrderConsumer가 있는 assembly내의 모든 consumer를 추가. 
                        // IConsumer<T> 및 IConsumerDefinition<T> 를 모두 찾아서  configurator.AddConsumer() 한다...
                        configurator.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                        
                        // Saga 사용하려면 이게 필요.
                        configurator
                            .AddSagaStateMachine<OrderStateMachine, OrderState>(typeof(OrderStateMachineDefinition))
                            //.InMemoryRepository() --> 이렇게 하면, 프로세스가 죽을때 상태정보도 날라감. Redis, SQL, MongoDB 같은걸 써야 댐. 
                            .RedisRepository(s =>
                            {
                                s.DatabaseConfiguration("127.0.0.1"); // 복잡한 설정은 ConfigurationOptions 객체를 사용. 
                                
                                //  동일한 CorrelationId 를 가지는 메시지가 사방에서 동시에 날아들면...--> Concurrency Issue. 가 있을 수 있다.
                                // 이런 경우,
                                //     1) Backend(ex: Redis) 쪽에서 Lock 을 가지게 하던지(한번에 하나씩만 처리된다)
                                //     2) Partitioner 를 사용하여 해결. (@partitioner 주석 및 https://masstransit-project.com/usage/sagas/guidance.html 참고)
                                //s.ConcurrencyMode =ConcurrencyMode.Pessimistic
                            })
                            ;
                        
                        // 일종의 Mediator 역할을 하는 Bus 를 추가. (즉, configurator.AddInMemoryBus(); 대신 아래처럼 하면 Inter-process Comm이 된다.)
                        configurator.AddBus(ConfigureBus);
                    });
                    services.AddHostedService<MassTransitConsoleHostedService>();
                });

        private static IBusControl ConfigureBus(IServiceProvider serviceProvider)
        {
            return Bus.Factory.CreateUsingRabbitMq(configurator =>
            {
                configurator.Host("rabbitmq://admin:mirero@localhost:5672");
                configurator.ConfigureEndpoints(serviceProvider);
                
                // 사용자가 명시적으로 임의 EndPoint 를 만들고 설정가능.
                // configurator.ReceiveEndpoint("something-else", e =>
                // {
                //     e.UseMessageRetry(r => r.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5)));
                //     //e.ConfigureConsumer(serviceProvider, typeof(SubmitOrderConsumer));
                //     //e.ConfigureSaga(serviceProvider, typeof(OrderStateMachine));
                // });
            });
        }
    }
}