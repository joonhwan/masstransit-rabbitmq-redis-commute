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
using Sample.Components;
using Sample.Contracts;

namespace Rabbit.Details
{
    public class MasstransitBasicStyleUsage : IRabbitMqUsage
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
            
            // 각 Message별 Publish용 Exchange 의 이름도 바꿀 수 있다....지만, 메시지 전송쪽에서도 이름을 알고 있어야 한다.
            // cfg.Message<UpdateAccount>(m => m.SetEntityName("update-account-cmd"));
            // cfg.Message<DeleteAccount>(m => m.SetEntityName("delete-account-cmd"));
            // 
            // 일괄로 어떤 Entity Name Formatter 를 하나 만들고, 시스템 전체에서 공유하는 방법도 있음. 
            // cfg.MessageTopology.SetEntityNameFormatter(new MyFormatter());
            
            // ******* CASE -1  : 이름없는(이름이 각 서비스마다 고유한...) Endpoint
            //               ---> Pub/Sub ( Message Broadcasting ) 
            //
            //   {Computer명칭}-{프로그램명칭}-{Hash} 로 이루어진 Exchange 및 Queue가 생성(AutoDelete = True다)
            //  --> AccountConsumer가 소비하는 `Sample.Contracts:UpdateAccount` 의 Message Exchange도 함께 생성되며 Bind 됨.
            //  --> 프로그램이 멈추면 제거되는 Exchange/Queue 
            // ******************************
            // cfg.ReceiveEndpoint(e =>
            // {
            //     e.Consumer<UpdateAccountConsumer>();
            //     e.Consumer<DeleteAccountConsumer>();
            // });

            // ******* CASE - 2  : 이름있는 Endpoint .
            //                    --> Message Handler(Message 가 Load Balancing)
            //
            //   주어진 명칭으로 이루어진 Exchange 및 Queue가 생성(AutoDelete는 False 인 Temporary한 Exchange/Queue Pair)
            //  --> AccountConsumer가 소비하는 `Sample.Contracts:UpdateAccount` 의 Message Exchange도 함께 생성되며 Bind 됨.
            //  --> 프로그램이 멈추어도 남아있음.
            // ******************************
            cfg.ReceiveEndpoint("account-service",
                e =>
                {
                    e.Consumer<UpdateAccountConsumer>();
                    e.Consumer<DeleteAccountConsumer>(); // 이렇게 하나 더 넣을 수 도 있다.
                }
            );

            // ****** CASE - 3 ********
            // Consumer 이름으로 된 Endpoint를 만들 수도 있다. ;
            //            --> 주로 Endpoint 1개 당 Consumer 1개 로 처리하는 경우?!?
            //
            // var nameOf = KebabCaseEndpointNameFormatter.Instance;
            // var consumerName = nameOf.Consumer<UpdateAccountConsumer>();
            // Console.WriteLine("ConsumerName : {0}", consumerName);
            // cfg.ReceiveEndpoint(consumerName,
            //     e =>
            //     {
            //         e.Consumer<UpdateAccountConsumer>();
            //     });
        }

        public async Task Test(IBusControl busControl)
        {
            await busControl.Publish<UpdateAccount>(new
            {
                AccountNumber = "update me.",
            });
            await busControl.Publish<DeleteAccount>(new
            {
                AccountNumber = "delete me."
            });
        }
    }

    public class MyFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>()
        {
            return "message:" + typeof(T).Name;
        }
    }
}