using System;
using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit.Definition;
using MassTransit.MongoDbIntegration;
using MassTransit.RabbitMqTransport;
using MassTransit.RedisIntegration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Warehouse.Components.Consumers;
using Warehouse.Components.CourierActivities;
using Warehouse.Components.StateMachines;
using Warehouse.Contracts;
using IHost = Microsoft.Extensions.Hosting.IHost;
namespace Warehouse.Service
{
    static class Program
    {
        public static async Task Main(string[] args)
        {
            const int eucKrCodePage = 51949; // euc-kr 코드 번호
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var eucKr = Encoding.GetEncoding(eucKrCodePage);
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("warehouse.service-.log", rollingInterval: RollingInterval.Day, encoding: eucKr)
                .CreateLogger();
            
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
            
            Log.CloseAndFlush();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    // logging.AddConsole(options =>
                    // {
                    //     options.TimestampFormat = "[HH:mm:ss] ";
                    // });
                    logging.AddSerilog(dispose: true);
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                })
                .ConfigureServices((hostContext, services) =>
                {
                    //services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance); --> 이렇게 해도 되고. @2 처럼 해도 되고? 
                    services.AddMassTransit(configurator =>
                    {
                        configurator.SetKebabCaseEndpointNameFormatter(); // @2 --> @! 처럼 해도되고?! 이게 더 읽기 편하네.
                        
                        // masstransit consumer 등록
                        configurator.AddConsumersFromNamespaceContaining<AllocateInventoryConsumer>();
                        // masstransit courier 를 위한 activity 등록.
                        configurator.AddActivitiesFromNamespaceContaining<AllocateInventoryActivity>();
                        // masstransit saga 등록
                        configurator
                            .AddSagaStateMachine<AllocationStateMachine, AllocationState>(typeof(AllocationStateMachineDefinition))
                            .MongoDbRepository(x =>
                            {
                                x.Connection = "mongodb://localhost:27017";
                                x.DatabaseName = "allocationDb";
                                // Collection이름을 명시적으로 줄 수도 있다.
                                // --> 주지 않으면 State를 지정한 Type의 이름(=`OrderState`)으로 부터 추론(="order.state")
                                //x.CollectionName = "orderState"
                            })
                            ;
                        
                        // Saga 사용하려면 이게 필요.
                        // configurator
                        //     .AddSagaStateMachine<OrderStateMachine, OrderState>(typeof(OrderStateMachineDefinition))
                        //     .MongoDbRepository(x =>
                        //     {
                        //         x.Connection = "mongodb://localhost:27017";
                        //         x.DatabaseName = "orderDb";
                        //         // Collection이름을 명시적으로 줄 수도 있다.
                        //         // --> 주지 않으면 State를 지정한 Type의 이름(=`OrderState`)으로 부터 추론(="order.state")
                        //         //x.CollectionName = "orderState"
                        //     })
                        //     ;
                        
                        // AllocateInventoryActivity 가 IRequestClient<AllocateInventory> 를 필요로 함.
                        configurator.AddRequestClient<AllocateInventory>();
                        
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
                
                // Warehouse.Service 에서 실행되는 Saga Statemachine 이 Schedule 기능을 사용. 
                // --> Schedule 된 메시지를 어디로 보내야 하는지 여기서 설정.(전송된 메시지는 Sample.Quartz.Service에서 수신하여, 필요한 곳으로 relay?)
                //  (참고: https://masstransit-project.com/advanced/scheduling/ )
                configurator.UseMessageScheduler(new Uri("queue:quartz-scheduler"));
                
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