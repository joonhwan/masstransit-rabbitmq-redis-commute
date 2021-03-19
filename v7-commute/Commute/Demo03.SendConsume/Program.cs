using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommuteSystem.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo03.SendConsume
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
                    services.AddHostedService<HostedServiceSend>();
                    services.AddHostedService<HostedServiceConsume>();

                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<ClaimSubmissionConsumer>()
                            .Endpoint(e =>
                            {
                                e.Name = "mirero-claim-submission-consumer-service";
                                
                                // Temporary EndPoint 란, Consumer가 연결이 전혀 되지 않는 상태가 되면 Consumer의 Exchange와 Queue가 사라지는 Exchange / Queue
                                e.Temporary = true;
                                
                                // ConsumeTopology 란, Message Exchange -> Consumer Exchange -> Consumer Queue 가 만들어지는걸 말한다. 
                                // ConsumeTopology 를 Configure 하지 않게 하면,
                                // ClaimSubmissionConsumer가 처리하는 Message Exchange가 생성되지 않는다.
                                // --> 오직 Consumer Exchange 와 Consumer Queue 만 만들어진다. 
                                e.ConfigureConsumeTopology = false;  
                            });

                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.ConfigureEndpoints(context);
                        });
                    });
                });
    }
}