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
using MassTransit.MongoDbIntegration;
using MassTransit.RabbitMqTransport;
using MassTransit.RedisIntegration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sample.Components.Consumers;
using Sample.Components.StateMachines;
using Sample.Components.StateMachines.OrderStateMachineActivities;
using Warehouse.Components.Consumers;
using Warehouse.Components.CourierActivities;
using Warehouse.Contracts;
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
                    // configurator.AddActivity() 로 할 수 없는 Automatanous 의 Activity 는 이런식으로...
                    //   ---> Statemachine 에서... x.OfType<AcceptOrderActivity() 부분이 동작하려면.. 이렇게 해야 됨.
                    services.AddScoped<AcceptOrderActivity>();
                    
                    //services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance); --> 이렇게 해도 되고. @2 처럼 해도 되고? 
                    services.AddMassTransit(configurator =>
                    {
                        configurator.SetKebabCaseEndpointNameFormatter(); // @2 --> @! 처럼 해도되고?! 이게 더 읽기 편하네.
                        // SubmitOrderConsumer가 있는 assembly내의 모든 consumer를 추가. 
                        // IConsumer<T> 및 IConsumerDefinition<T> 를 모두 찾아서  configurator.AddConsumer() 한다...
                        configurator.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();

                        {
                            // Courier 를 사용하기 위해...
                            //  --> 음. 이렇게 되면, Sample.Xxxx 하는 시스템은 Warehouse.Xxxx 에 의존성이 생김.
                            configurator.AddConsumersFromNamespaceContaining<AllocateInventoryConsumer>();
                            configurator.AddActivitiesFromNamespaceContaining<AllocateInventoryActivity>();
                        }


                        // Saga 사용하려면 이게 필요.
                        configurator
                            .AddSagaStateMachine<OrderStateMachine, OrderState>(typeof(OrderStateMachineDefinition))
                            // Saga 저장소 Backend를 여기서 설정한다 
                            // 
                            // Case 1 : InMemory 저장소
                            //
                            // .InMemoryRepository() --> 이렇게 하면, 프로세스가 죽을때 상태정보도 날라감. Redis, SQL, MongoDB 같은걸 써야 댐.
                            //
                            // Case 2 : Redis 저장소.
                            //          Correlation Id 하나만 가지고 Correlation 하는 경우에는 Redis 도 OK.
                            //          하지만, 제3의 상태값으로 Correlate 하려면, 다른 Backend를 써야 함.
                            //
                            // .RedisRepository(s =>
                            // {
                            //     s.DatabaseConfiguration("127.0.0.1"); // 복잡한 설정은 ConfigurationOptions 객체를 사용. 
                            //     
                            //     //  동일한 CorrelationId 를 가지는 메시지가 사방에서 동시에 날아들면...--> Concurrency Issue. 가 있을 수 있다.
                            //     // 이런 경우,
                            //     //     1) Backend(ex: Redis) 쪽에서 Lock 을 가지게 하던지(한번에 하나씩만 처리된다)
                            //     //     2) Partitioner 를 사용하여 해결. (@partitioner 주석 및 https://masstransit-project.com/usage/sagas/guidance.html 참고)
                            //     //s.ConcurrencyMode =ConcurrencyMode.Pessimistic
                            // })
                            //
                            // Case 3 : MongoDB 저장소 
                            .MongoDbRepository(x =>
                            {
                                x.Connection = "mongodb://localhost:27017";
                                x.DatabaseName = "orderDb";
                                // Collection이름을 명시적으로 줄 수도 있다.
                                // --> 주지 않으면 State를 지정한 Type의 이름(=`OrderState`)으로 부터 추론(="order.state")
                                //x.CollectionName = "orderState"
                            })
                            ;
                        
                        configurator.AddRequestClient<AllocateInventory>(); // 이거 왜 필요하지 ?
                        
                        // 일종의 Mediator 역할을 하는 Bus 를 추가.
                        // configurator.AddInMemoryBus(); // in-memory bus. 프로세스간 통신 X 
                        configurator.AddBus(ConfigureBus); // out-of-process bus. 브로커(rabbitmq, azure service ubs...) 프로세스 통신 O
                    });
                    
                    // 윈도우 서비스 처럼 Start/Stop 을 가지는 Service 객체를 추가.
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