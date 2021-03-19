using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;

namespace SubmitClaim.Client
{
    class Program
    {
        public class Command : CommuteSystem.Contracts.SubmitClaim
        {
            public Guid CustomerId { get; set; }
            public Guid OrderId { get; set; }
            public string ClaimContents { get; set; }
            public int DegreeOfHardness { get; set; }
        }

        static async Task Main(string[] args)
        {
            // Client의 경우 RabbitMQ 에 특별한 Endpoint를 정의하지 않고, 
            // 단순히 Message 를 보내기만 한다. 따라서, Bus 만 만들면 된다. 
            IBusControl busControl = Bus.Factory.CreateUsingRabbitMq();

            await busControl.StartAsync();

            try
            {
                await busControl.Publish<CommuteSystem.Contracts.SubmitClaim>(new Command
                {
                    ClaimContents = Repeat("블라", new Random().Next(1, 10)),
                    CustomerId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    DegreeOfHardness = 0
                });
            }
            finally
            {
                await busControl.StopAsync();                
            }
        }
        
        static string Repeat(string s, int n)
        {
            return new StringBuilder(s.Length * n)
                .AppendJoin(s, new string[n+1])
                .ToString();
        }
    }
}