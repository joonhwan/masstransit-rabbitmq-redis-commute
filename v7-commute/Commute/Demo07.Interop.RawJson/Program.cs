using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo07.Interop.RawJson.Consumers;
using Demo07.Interop.RawJson.Messages;
using Demo07.Interop.RawJson.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo07.Interop.RawJson
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
                    
                    // 이 Demo 를 위해 Consumer에서 사용할 Repository 를 주입.
                    services.AddScoped<IDeviceDataRepository, InMemoryDeviceDataRepository>();
                    
                    services.AddMassTransit(x =>
                    {
                        // 외부 시스템으로 RabbitMQ 를 통해 json 메시지를 수신하는 Consumer.
                        x.AddConsumer<UpdateLocationConsumer>(typeof(UpdateLocationConsumerDefinition));
                        
                        x.UsingRabbitMq((context, configurator) =>
                        {
                            configurator.ConfigureEndpoints(context);
                        });
                    });
                });
    }
}