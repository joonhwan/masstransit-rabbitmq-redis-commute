using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Sample.Platform.Commands;

namespace Sample.Platform.Commands
{
    public interface SampleCommand
    {
        Guid CommandId { get; }
        string Command { get; } 
    }
}

namespace Sample.Platform.Client
{
    
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                
            });

            await bus.StartAsync();

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        Console.WriteLine("명령을 입력해 보세요. 그냥 엔터 치면 작업을 모두 중단합니다.");
                        var line = Console.ReadLine();

                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }

                        Console.WriteLine("명령을 전송합니다 : '{0}'", line);

                        
                        var message = new
                        {
                            CommandId = InVar.Id,
                            Command = line
                        };
                        await bus.Publish<SampleCommand>(message);
                    }
                });
            }
            finally
            {
                await bus.StopAsync();
            }
        }
    }
}