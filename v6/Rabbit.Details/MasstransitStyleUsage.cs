using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Sample.Components;
using Sample.Contracts;

namespace Rabbit.Details
{
    public class MasstransitStyleUsage : IRabbitMqUsage
    {
        public void Configure(IRabbitMqBusFactoryConfigurator cfg)
        {
            cfg.Host("localhost",
                h =>
                {
                    h.Username("guest");
                    h.Password("guest");

                    // h.UseSsl(x =>
                    // {
                    //     x.Certificate = new X509Certificate();
                    //     x.Protocol = SslProtocols.Tls12;
                    //     x.ServerName = "SecureRabbit";
                    // });

                    // h.UseCluster(x =>
                    // {
                    //     x.Node("192.168.100.11");
                    //     x.Node("192.168.100.12");
                    // });
                }
            );
            
            cfg.ReceiveEndpoint("account-service",
                e =>
                {
                    // Exchange - Queue 의 연결 Topology를 맘대로 주무른다.
                    // ... 라고 할 수는 있지만, 여러 consumer 들에 공유되는 메시지와의 topology 구성은 
                    //     수동으로 하게 되면, 한 consumer는 topoplogy 를 A 유형으로 설정하고,
                    //     다른 consumer는 topology 를 B 유형으로 설정하는 식의 충돌이 발생할 가능성이 높아진다
                    //      --> 이렇게 되면 process가 잠시 hang 되다가 예외가 발생한다. 
                    {
                        // 이렇게 하면 Message Type별 Exchange 와 'account-service' 가 bind 되지 않는다.
                        // --> 즉, `account-service` 는 더이상 Publish<UpdateAccount>() 에 의해 메시지를 수신 못한다.
                        e.ConfigureConsumeTopology = false;

                        // 위에서 consume topology 를 False 로 해서 자동 Bind 되는것은 disable 했지만, 
                        // 아래 처럼 명시적으로 Bind 할 수 있다. 즉, `UpdateAccount` 에 현 endpoint인 `account-service` exchange 를 bind 한다. 
                        // e.Bind<UpdateAccount>(x =>
                        // {
                        //     // UpdateAccount 메시지 Exchange --> 'account-service' Exchange 를 bind 할 때의 설정.
                        //     x.RoutingKey = "BlaBla"; // Exchange Type 이 fanout 인 경우에는 무효하다. 
                        //     //x.Durable = true;
                        //     //x.AutoDelete = false;
                        //     //x.ExchangeType = ExchangeType.Direct; // --> UpdateAccount 메시지 Exchange의 Type 을 변경. 
                        // });
                    }

                    e.Durable = true; // 기본값이 true
                    //e.Exclusive = true; // true 면 다른 서비스는 접근 안하는 end point
                    e.Lazy = true; // true 면 메모리 보다 파일로 메시지들이 기록된다. 
                    //e.AutoDelete = true; // true 면 이 서비스가 죽으면 해당 end point가 사라진다.
                    //e.ConsumerPriority = 1234; // 이 숫자가 큰 서비스쪽으로 메시지가 주로 전달된다.???

                    // string[] exchangeTypes =
                    // {
                    //     ExchangeType.Direct, ExchangeType.Fanout, ExchangeType.Headers, ExchangeType.Topic
                    // };
                    // e.ExchangeType = exchangeTypes[0];

                    //e.ExclusiveConsumer = true;  // Consumer 만 배타접근. 단 넘들은 접근 X ???

                    //var inputAddress = e.InputAddress; // ReadOnly 값. 
                    e.PrefetchCount = 20; // 기본값은 CPU 논리갯수. 동시에 처리 가능한 메시지....와 관련이 있음.  중요한 설정값임.

                    // QOS 에 대한 설명도 있었지만 이해못함. 

                    e.Consumer<UpdateAccountConsumer>(c =>
                    {
                        // 마저.. 이런식으로 Message Retry 관련 처리도 할 수 있었지.
                        // c.UseMessageRetry(r =>
                        // {
                        //     r.Interval(3, TimeSpan.FromSeconds(1));
                        // });
                    });
                });
            //
            // cfg.ReceiveEndpoint("another-account-service",
            //     e =>
            //     {
            //         e.PrefetchCount = 10;
            //         e.Consumer<AnotherAccountConsumer>();
            //     });

        }

        public async Task Test(IBusControl busControl)
        {
            //var endPoint = await busControl.GetSendEndpoint(new Uri("exchange:account-service"));
            // await endPoint.Send<UpdateAccount>(new
            // {
            //     AccountNumber = "12345",
            // });
            await busControl.Publish<UpdateAccount>(new
            {
                AccountNumber = "abcde",
            });
        }
    }
}