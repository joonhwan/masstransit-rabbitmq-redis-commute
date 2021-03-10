using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Specifications;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
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

    public class TrashConsumerDefinition : ConsumerDefinition<TrashConsumer>
    {
        public TrashConsumerDefinition()
        {
            
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TrashConsumer> consumerConfigurator)
        {
            // 세상에서 ConsumeContext<TMessage> 에 대한 Filter를 만들려면..... 이렇게 복잡해야 한다. 😪
            endpointConfigurator.ConnectConsumerConfigurationObserver(
                new ConsoleConsumeMessageFilterConfigurationObserver(endpointConfigurator)
            );
            
            endpointConfigurator.UseFilter(new ConsoleConsumeFilter());
            
            consumerConfigurator.UseFilter(new ConsoleConsumeWithConsumerFilter<TrashConsumer>());
            
            consumerConfigurator.ConsumerMessage<EmptyTrashBin>(m =>
                m.UseFilter(new ConsoleConsumeWithConsumerAndMessageFilter<TrashConsumer, EmptyTrashBin>())
            );
        }
    }

    
}