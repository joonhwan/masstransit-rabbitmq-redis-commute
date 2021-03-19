using System;
using System.Threading.Tasks;
using CommuteSystem.Consumers;
using CommuteSystem.Contracts;
using CommuteSystem.StateMachines;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfigureEndpointsDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    
                    ConfigureMassTransit(services);
                });

        private static void ConfigureMassTransit(IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                // Endpoint 는 메시지 수신을 위한 연결점을 의미. `AddMasstransit()` 의 역할은 메시지의 수신의 연결점들을 정의하고 구성함
                // ---> "Topology를 구성한다" 고 표현됨.
                // ---> RabbitMQ 로 생각하자면, Endpoint는 Queue로 생각하는게 편함(하지만, Masstransit은 동일한 이름의 Exchange를 항상 만들어둠)
                // ---> Topology가 구성되면, RabbitMQ의 경우 Message Exchange, Consumer Exchange/Queue, Saga Exchange/Queue 들이 만들어지고 서로 연결(bind)됨. 
                
                // -- Consumer(메시지 핸들러 클래스)들은 RabbitMQ, AzureServiceBus, ActiveMQ 의 Backend Broker보다 더 포괄적임. 
                //    따라서, Masstransit 기본 라이브러리 수준에서 Consumer가 Endpoint에 등록가능.
                //       --> 나중에 각 Backend 기술별 설정시, 여기 등록된 Consumer들이 한방에 등록 가능
                //            (아래 @configure-endpoints-automatically 참고)
                //       --> 등록 될 때는 RabbitMQ의 경우...
                //              - 각 Consumer가 소비하는 Message Type 별 Exchange
                //              - 각 Consumer별 Queue(및 Bind된 Exchange)
                //           가 생성됨.
                {
                    x.AddConsumer<OrderSubmissionConsumer>();

                    x.AddConsumer<AuditOrderConsumer>(typeof(AuditOrderConsumerDefinition))
                        .Endpoint(e => { e.Name = "audit-order-service"; });

                    x.AddConsumer<UpdateCacheConsumer, UpdateCacheConsumerDefinition>()
                        .Endpoint(e =>
                        {
                            e.Temporary = true;
                            e.InstanceId = ".temporal.v8"; // endpoint 명칭 뒤에 올 아무 문자열.
                        });
                }

                // -- Saga Statemachine 도 Backend Broker에 중립적. Consumer와 유사하게 처리하게 됨
                //      --> 아래 @configure-endpoints-automatically 부분에서 실제 Backend Broker 등록됨 
                x.AddSagaStateMachine<OrderTrackingStateMachine, OrderTrackingSaga>()
                        .InMemoryRepository() // 개발/테스트용 In Memory Saga 저장소.
                        ;
                
                // ---- EndPoint의 명칭을 제어할 수 있다. ----
                // x.SetKebabCaseEndpointNameFormatter();
                //x.SetEndpointNameFormatter(new CustomEndpointNameFormatter());
                
                // RabbitMQ Broker 를 등록
                //     - context : 앞에서 등록한 Consumer, Saga 등의 설정을 포함하고 있는 DI 컨테이너
                //     - configurator : Rabbit MQ 의 Bus 설정자 
                x.UsingRabbitMq((context, configurator) =>
                {
                    // --- Rabbit 연결은 다양하게... 가능 . 기본값은 localhost:5672 로 guest/guest 계정.
                    //       - 단일 연결
                    //       - 클러스터 연결 
                    //       - 보안 관련사항
                    //       - Batch Publish 설정(작은 메시지를 수천번 보내는 것 보다는 한번에 묶에서 보냄)
                    //        .... 등..
                    // 
                    // configurator.Host("192.168.100.115:5672",
                    //     host =>
                    //     {
                    //         host.Username("admin");
                    //         host.Password("mirero");
                    //         host.UseCluster(cluster =>
                    //         {
                    //             cluster.Node("192.168.100.115:5672");
                    //             cluster.Node("192.168.100.116:5672");
                    //             cluster.Node("192.168.100.117:5672");
                    //         });
                    //         host.ConfigureBatchPublish(batch =>
                    //         {
                    //             batch.Enabled = true;
                    //             batch.Timeout = TimeSpan.FromSeconds(1);
                    //             batch.MessageLimit = 16;
                    //             batch.SizeLimit = 10_000_000;
                    //         });
                    //     });
                            
                    // --- Message 별 Exchange 의 정의는  다음의 방식들이 가능.
                    // 방법1)
                    //    Message Class에 [EntityName("Exchange명칭")] 을 사용 --> @what-is-message-entity-name 참고
                    // 방법2)
                    //   configurator.Message<SubmitClaim>(c => c.SetEntityName("Mireo.Command.SubmitClaim"));
                    // 방법3)
                    //  configurator.MessageTopology.SetEntityNameFormatter(new CustomEntityNameFormatter());

                    
                    // 간단하게 endpoint 들을 rabbit mq 쪽에 한방에 설정하는 편이함수.
                    //  - Consumer 를 위한 endpoint,
                    //  - Saga 를 위한 endpoint,
                    //  - ... 
                    //  같은 걸 Dependency Injection 에 등록된 정보를 기준으로 쭈욱 RabbitMQ 에 Exchange/Queue 로 정의하고 연결한다.
                    //
                    //  따라서, Exchange와 Queue의 명칭에 대한 규칙정보 DI 에 주입된
                    // IEndpointNameFormatter 구현체를 사용하며, 기본값은 .NET Type Name 이다.
                    configurator.ConfigureEndpoints(context); // @configure-endpoints-automatically
                    
                    
                    //  --- RabbitMQ 의 Configurator(~`configurator` 인자)를 사용해
                    //     RabbitMQ 인프라입장에서 수동 Endpoint 등록하는 것도 가능..
                    //            --> 지정된 이름의 Queue 가 만들어짐(이 Queue에 bind 된 Exchange도 함께).
                    configurator.ReceiveEndpoint("mirero-provider-service", e =>
                    {
                        // -- "mirero-provider-service" 의 queue 
                        // e.AutoDelete = false;
                        // e.Durable = true;
                        // e.PrefetchCount = 16;
                        
                        // Endpoint 1개에 여러개의 Consumer를 연결할 수 있음. 
                        e.ConfigureConsumer<AuditOrderConsumer>(context);
                        e.ConfigureConsumer<UpdateCacheConsumer>(context,
                            c =>
                            {
                                // 각 consumer 별 설정 도 가능.
                                // c.UseMessageRetry(r => r.Interval(3, 10_000));
                            });
                    });
                    
                    // 무명의 Endpoint로 만들 수 있음. --> 프로세스가 죽으면 Endpoint는 제거됨
                    //  --> RabbitMQ 식으로 말하자면, 관련 Exchange/Queue가 제거됨
                    configurator.ReceiveEndpoint(e =>
                    {
                        e.Handler<OrderAudited>(c =>
                        {
                            Console.WriteLine("OrderAudited 수신됨");
                            return Task.CompletedTask;
                        });
                    });
                });
            });
        }
    }
}