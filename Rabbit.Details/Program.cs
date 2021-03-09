using System;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using RabbitMQ.Client;
using Sample.Components;
using Sample.Contracts;

namespace Rabbit.Details
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var usage
                    = new MasstransitDirectExchangeUsage()
                    //= new MasstransitBasicStyleUsage()
                    //= new MasstransitStyleUsage()
                    //= new MoreRabbitMqStyleUsage()
                ;
            Console.WriteLine("Usage Class : {0}", usage.GetType());
            
            var busControl 
                //= Bus.Factory.CreateUsingRabbitMq(ConfigureMasstransitConfirmatively);
                = Bus.Factory.CreateUsingRabbitMq(usage.Configure);

            // CT 를 활용하는 방법....중 하나. 또 Service Class에서 반응성을 높이려면, CT 가 반드시 필요.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));  // 30초 뒤 Cancel 
            await busControl.StartAsync(cts.Token);
            try
            {
                Console.WriteLine("Bus was Created! something to Test, empty to quit.");

                while (true)
                {
                    await usage.Test(busControl);

                    // Console.ReadLine(); // --> 대기 한답시고, 이렇게 하면 안된다. --> 전체 쓰레드를 전부 block 한다.
                    var line = await Task.Run(() => Console.ReadLine()); // --> 꼭 이렇게 해야 한다.
                    if (string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                }
            }
            finally
            {
                await busControl.StopAsync(CancellationToken.None);
            }
        }
    }
}
