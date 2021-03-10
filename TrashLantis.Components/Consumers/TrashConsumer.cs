using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using TrashLantis.Contracts;

namespace TrashLantis.Components.Consumers
{
    public class TrashConsumer : IConsumer<EmptyTrashBin>
    {
        private readonly ILogger<TrashConsumer> _logger;

        public TrashConsumer(ILogger<TrashConsumer> logger)
        {
            _logger = logger;
        }
        
        public async Task Consume(ConsumeContext<EmptyTrashBin> context)
        {
            _logger.LogInformation("쓰레기통을 비웁니다 : BinNumber = {BinNumber}", context.Message.BinNumber);
            
            await Task.Delay(1000);
            
            _logger.LogInformation("쓰레기통을 비웠습다 : BinNumber = {BinNumber}", context.Message.BinNumber);
        }
    }
}