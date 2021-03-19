using System;
using System.ComponentModel.Design;
using CommuteSystem;
using CommuteSystem.Consumers;
using CommuteSystem.Contracts;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PublishDemo
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
                        // Consumer를 등록. 
                        x.AddConsumer<ClaimSubmissionConsumer>();
                        
                        x.UsingRabbitMq((context, configurator) =>
                        {
                            // --- Rabbit 연결은 다양하게... 가능 . 기본값은 localhost:5672 로 guest/guest 계정.
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

                            
                            //  --- RabbitMQ의 Alternate Exchange 도 가능.
                            configurator.Publish<SubmitClaim>(p =>
                            {
                                // 아래 처럼하면, Consumer가 `SubmitClaim` 을 수신하지 않는 경우, 이리로 빠진다.? 
                                p.BindAlternateExchangeQueue("alternate-exchange", "alternate-queue");
                            });
                            
                            configurator.ConfigureEndpoints(context);
                        });
                    });
                });
    }
}