using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sample.Platform.Commands;

namespace Sample.Platform.Consumers
{
    public class SampleConsumer : IConsumer<SampleCommand>
    {
        private readonly ILogger<SampleConsumer> _logger;

        public SampleConsumer(ILogger<SampleConsumer> logger)
        {
            _logger = logger;
        }
        
        public async Task Consume(ConsumeContext<SampleCommand> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Sample Command({Id}) : {Command} 작업을 시작합니다", msg.CommandId, msg.Command);

            await Task.Delay(3000);
            
            _logger.LogInformation("Sample Command({Id}) : {Command} 작업을 완료했습니다", msg.CommandId, msg.Command);
        }
    }
}