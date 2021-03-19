using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommuteSystem.Consumers;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo05.Retry
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
                        // NOTE: Message Retry는 프로세스에서 재시도함. 따라서, 재시도 중 프로세스가 날아가면,
                        //      메시지는 다시 원래의 Endpoint Queue로 돌아가고, 재시도 횟수가 리셋됨. 
                        //      재시도중 Message는 RabbitMQ의 경우, "처리중" 으로 표시됨. 
                        //      
                        //      Retry 재시도 간격이 몇시간, 몇일, 몇달...수준의 긴 기간이면,
                        //     "Retry" 가 아니라 "Redeliver" 라는 개념이 필요할 수도 있음.
                        //      --> Quartz, Hangfire 같은 Scheduler 및 이와 통합된 별도 Masstransit 라이브러리가 필요함.
                        //          https://masstransit-project.com/usage/exceptions.html#redelivery
                        
                        
                        // -- 가장 간단한 Retry  설정방법 .
                        //
                        // x.AddConsumer<ClaimSubmissionConsumer>(c =>
                        // {
                        //     c.UseMessageRetry(r => r.Intervals(10_000, 30_000, 60_000));
                        // });
                        
                        // -- 좀더 체계적? 인 설정방법.
                        // ClaimSubmissionConsumerDefinition 에 정의된 Retry 설정 참고할만함. 
                        x.AddConsumer<ClaimSubmissionConsumer>(typeof(ClaimSubmissionConsumerDefinition)); // @general-consumer-register
                        
                        // Retry는 Consumer 수준, Endpoint 수준, Bus 수준 등 여러 단계에서 재시도를 설정할 수 있다고 함.
                        // (Consumer 수준이 가장 일반적이지 않을까...)
                        
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.ConfigureEndpoints(context); 

                            // Endpoint를 여러개 만들고, 메시지 1개에 대해 서로 다른 Consumer가 동시에 처리하는것도 가능.
                            // --> 각 Endpoint는 개별 Retry 설정이 있음. 
                            // 돌려보면, 3번 재시도하면 성공하게 되는 메시지가 각 endpoint에서 몇번 시도 후 *_error Queue 로 빠지는 지,
                            // 그리고, *_error Queue에 들어간 각 메시지의 Retry Attempt 는 몇번씩인지.. 확인해볼만함. 
                            // 
                            // ... 이렇게 개별 Endpoint 마다 동일한 Consumer를 만들어도, 여전히 @general-consumer-register 에서 등록한 Consumer도 별도로 존재한다.
                            //     
                            for (var i = 0; i < 3; ++i)
                            {
                                var maxRetry = i + 1;
                                // 죄대 maxRetry 하는 Endpoint 생성.
                                cfg.ReceiveEndpoint($"claim-submission-with-{maxRetry}-retry",
                                    e =>
                                    {
                                        e.UseMessageRetry(r => r.Interval(maxRetry, 10_000));
                                        e.ConfigureConsumer<ClaimSubmissionConsumer>(context);
                                    });
                            }
                        });
                    });
                });
    }
}