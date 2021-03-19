using System.Threading.Tasks;
using CommuteSystem.Contracts;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;

namespace CommuteSystem.Consumers
{
    public class UpdateCacheConsumer : IConsumer<UpdateCache>
    {
        private readonly ILogger<UpdateCacheConsumer> _logger;

        public UpdateCacheConsumer(ILogger<UpdateCacheConsumer> logger)
        {
            _logger = logger;
        }
        
        public async Task Consume(ConsumeContext<UpdateCache> context)
        {
            var message = context.Message;
            
            _logger.LogInformation("UpdateCache 처리시작 : {ProductId}", message.ProductId);
            await Task.Delay(1000);
            _logger.LogInformation("UpdateCache 처리완료 : {ProductId}", message.ProductId);

        }
    }

    public class UpdateCacheConsumerDefinition : ConsumerDefinition<UpdateCacheConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<UpdateCacheConsumer> consumerConfigurator)
        {
            //TODO
        }
    }
}