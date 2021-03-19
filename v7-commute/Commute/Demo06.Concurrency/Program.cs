using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommuteSystem.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo06.Concurrency
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
                    services.AddHostedService<HostedServiceConsume>();
                    services.AddHostedService<HostedServicePublish>();

                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<AuditOrderConsumer>()
                            .Endpoint(e =>
                            {
                                // -- Prefetch Count : Consumer가 Endpoint 로 부터 동시에 수신할 메시지 최대갯수
                                // 기본값은 논리 CPU의 갯수. 이 값을 너무 크게 하면, Worker Service 다수개 운영할 때 효율이 안좋아 질 수 있다. 
                                // e.PrefetchCount = 4;

                                // 주의 : PrefetchCount 는 "메시지 동시 수신 갯수". 수신한 갯수만큼 동시에 Consumer가 항상되지는 않는다(CPU 갯수의 제한등)
                                //       따라서, 통상 PrefetchCount 보다는 "메시지 동시 처리 갯수" 인 ConcurrentMessageLimit 을 설정한다. 
                                
                                // -- Concurrent Message Limit : 메시지 동시 처리 최대 갯수. 
                                // 최대 10 개의 Consumer가 이 프로세스에서 메시지를 처리.
                                // --> 이 경우, RabbitMQ 의 Prefetch Count는 2 보다 같거나 큰 값으로 설정된다. 
                                e.ConcurrentMessageLimit = 10; 
                                
                                // 주의2: PrefetchCount 와 ConcurrentMessageLimit 을 함께 설정하기 보다는
                                //        ConcurrentMessageLimit 으로만 동시성을 설정하는 편이 합리적이다. 
                            });
                        
                        // 위 처럼 Fluent 하게 할 수 도 있지만, 좀 더 체계적인 Concurrency 설정을 위해서는 
                        // ConsumerDefinition<T> 를 사용하는방법이 권장
                        x.AddConsumer<AuditOrderConsumer>(typeof(AuditOrderConsumerDefinition));
                        
                        
                        
                        x.UsingRabbitMq((context, configurator) =>
                        {
                            configurator.ConfigureEndpoints(context);
                        });
                    });
                });
    }
}