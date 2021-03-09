using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Conductor.Configuration.Configurators;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTransit.Saga;
using MassTransit.Topology;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Sample.Components;
using Sample.Contracts;

namespace Rabbit.Details
{
    public class MasstransitDirectExchangeUsage : IRabbitMqUsage
    {
        public void Configure(IRabbitMqBusFactoryConfigurator cfg)
        {
            cfg.Host("localhost",
                h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                }
            );

            cfg.Lazy = true; // globally

            // publish side의 topology 구성
            cfg.Publish<UpdateAccount>(m =>
            {
                m.ExchangeType = ExchangeType.Direct;
                //m.BindQueue("some queue");
                
                // routing 되지 않은 것들을 모으는 exchange/queue pair를 만들고 현 message exchange에 대하여
                // alternate exchange 로 등록.
                m.BindAlternateExchangeQueue("unmatched-update-account");
            });
            
            // consume side의 topology 구성
            cfg.ReceiveEndpoint("account-service-a",
                e =>
                {
                    e.PrefetchCount = 10;

                    e.ConfigureConsumeTopology = false; // manually 하게...
                    e.Bind<UpdateAccount>(b =>
                    {
                        b.ExchangeType = ExchangeType.Direct;
                        b.RoutingKey = "A";
                    });
                    e.Consumer<UpdateAccountConsumer>();
                }
            );
            cfg.ReceiveEndpoint("account-service-b",
                e =>
                {
                    e.PrefetchCount = 10;
                    
                    e.ConfigureConsumeTopology = false; // manually 하게...
                    e.Bind<UpdateAccount>(b =>
                    {
                        b.ExchangeType = ExchangeType.Direct;
                        b.RoutingKey = "B";
                    });
                    e.Consumer<UpdateAccountConsumer>();
                }
            );
        }

        public async Task Test(IBusControl busControl)
        {
            await busControl.Publish<UpdateAccount>(new
            {
                AccountNumber = "update me. AAA",
            }, x =>
            {
                x.SetRoutingKey("A");
            });
            await busControl.Publish<UpdateAccount>(new
            {
                AccountNumber = "update me. BBB",
            }, x =>
            {
                x.SetRoutingKey("B");
            });
            await busControl.Publish<UpdateAccount>(new
            {
                AccountNumber = "update me. BBB",
            }, x =>
            {
                x.SetRoutingKey("C"); // routing key 지정이 안된거..
            });
        }
    }
}