using System;
using MassTransit;
using Sample.Contracts;

// 이 예제 코드에서 명시적으로 namespace 를 이렇게 지정한 이유는,
// namespace 가 exchange 명칭에 포함되기 때문. 
namespace Sample.Contracts 
{
    public interface UpdateAccount
    {
        string AccountNumber { get; }
    }
}

namespace Rabbit.Details.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host("localhost",
                    h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
            });

            while (true)
            {
                Console.WriteLine("AccountNumber to send and empty string to quit...");
                var line = Console.ReadLine();

                busControl.Publish<UpdateAccount>(new
                {
                    AccountNumber = line
                });
            }
        }
    }
}