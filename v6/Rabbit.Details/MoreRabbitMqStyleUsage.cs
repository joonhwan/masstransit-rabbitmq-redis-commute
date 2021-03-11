using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Sample.Components;
using Sample.Contracts;

namespace Rabbit.Details
{
    public class MoreRabbitMqStyleUsage : IRabbitMqUsage
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
            
            // ReceiveEndPoint = Exchange + Queue 쌍으로 만들어진다. 
            cfg.ReceiveEndpoint("account-service",
                e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Lazy = true;
                    e.PrefetchCount = 15;
                    
                    // 특정 Exchange와 Bind 할 수 있다. 
                    //e.Bind("update-account-command");
                    
                    // --> 들어온 메시지에 따라 1개 이상의 Consumer가 등록되며, Message Type에 따라 처리된다. 
                    e.Consumer<UpdateAccountConsumer>();
                    e.Consumer<DeleteAccountConsumer>(); //@delete-account
                    
                    // 만일 상기에 등록된 Consumer가 처리하는 메시지 타입 이외의 것이 들어오면 *_skipped Queue로 
                    // dead letter 처리가 된다.
                    
                });
            
            cfg.ReceiveEndpoint(e =>
            {
                e.Consumer<UpdateAccountSubscriber>();
            });
        }

        public async Task Test(IBusControl busControl)
        {
            await Task.Delay(0);
            // var endPoint = await busControl.GetSendEndpoint(new Uri("exchange:account-service")); // 또는 queue:account-service
            // await endPoint.Send<UpdateAccount>(new
            // {
            //     AccountNumber = "more rabbitmq way"
            // });
            //
            // // 이 메시지를 Consume 하는 넘이 없으면.. 즉, ... @delete-account 가 없으면,
            // // --> DeleteAccount는 'account-service' end point에서 dead letter 로 처리되어 
            // // `account-service_skipped` queue로 빠진다.
            // await endPoint.Send<DeleteAccount>(new
            // {
            //     AccountNumber = "i am DeleteAccount Command actually"
            // });
            
            // 여기서는 publish가 안된다. (왜냐하면, 
            // await busControl.Publish<UpdateAccount>(new
            // {
            //     AccountNumber = "published command!"
            // });
        }
    }
}