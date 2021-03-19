using System.Threading.Tasks;
using Demo07.Interop.RawJson.Messages;
using Demo07.Interop.RawJson.Services;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Logging;

namespace Demo07.Interop.RawJson.Consumers
{
    public class UpdateLocationConsumer : IConsumer<UpdateLocation>
    {
        private readonly IDeviceDataRepository _repository;
        private readonly ILogger<UpdateLocationConsumer> _logger;

        public UpdateLocationConsumer(IDeviceDataRepository repository, ILogger<UpdateLocationConsumer> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<UpdateLocation> context)
        {
            var message = context.Message;
            _logger.LogInformation("IOT Device 위치를 갱신합니다 : {DeviceId} @ ({x}, {y})",
                message.DeviceId,
                message.X,
                message.Y);
            
            _repository.SetLocation(message.DeviceId, (message.X, message.Y));
            
            _logger.LogInformation("IOT Device 위치를 갱신했습니다. : {DeviceId} @ ({x}, {y})",
                message.DeviceId,
                message.X,
                message.Y);

            return Task.CompletedTask;
        }
    }

    public class UpdateLocationConsumerDefinition : ConsumerDefinition<UpdateLocationConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<UpdateLocationConsumer> consumerConfigurator)
        {
            // 외부 시스템은 Masstransit의 Consumer Topology 를 모른다.
            //     (즉, Message Type 이름으로 된 Exchange -> Consumer Type 명으로 된 Exchange -> Consumer Type 명으로 된 Queue)
            // 외부시스템은. 단순히 "어떤 Queue 로 어떤 Message 를 보내" 만 원할 수 있다. 
            // 이런 경우 Topology를 무시할 수 있다.
            //  --> 아래 처럼하면, 단순히 Consumer Type명으로 된 Exchange/Queue 만 생기고,
            //      Message Type으로된 Exchange는 생성/Bind 되지 않는다.
            //  --> 외부시스템 뿐 아니라 Masstransit을 사용하는 내부시스템에서도 메시지를 보내려면...ConfigureConsumeTopology 는 놔두자.  
            // 
            // endpointConfigurator.ConfigureConsumeTopology = false;
            
            
            // --- 메시지 직렬화 포맷 문제..
            //
            // 기본적으로 Masstransit은 application/vnd.masstransit+json 형태의 **전용** Message Envelop Format을 가진다. 
            // 하지만, 이러한 envelop format을 모르는 외부시스템과 연동할 때는 단순한 형태의 json 을 필요가 있다. 
            // 이 경우, endpoint 에 대하여 RawJsonSerializer 를 지정해 줌으로 써 가능하다. 
            //
            // --> https://masstransit-project.com/architecture/interoperability.html 참고
            // 
            // 만일 외부 시스템에서 보내는 메시지만 받는다고 하면, `ClearMessageDeserializers()` + `UseRawJsonSerializer()` 를 함께 
            // 호출하여 "content_type=application/json" 과 같이 설정하지 않고, json 만 보내고 받는게 가능
            // 하지만, Masstransit에서도 메시지를 송신하길 원한 다면, `ClearMessageDeseriazliers()` 는 안하는게 좋을듯.
            //  (이 경우, 외부 시스템은 rabbitmq의 메시지 properties에 content_type=application/json 을 반드시 저정해야 됨)
            //endpointConfigurator.ClearMessageDeserializers();
            endpointConfigurator.UseRawJsonSerializer();

            // 아래는 RabbitMQ specific 한 설정이므로, .... 
            if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbitmq)
            {
                // 현 consumer 에 대한 endpoint를 "고정된 이름"의 exchange에 bind 시킨다.
                // 외부 시스템에서는 아래 Exchange 로 Message 를 Raw Json 으로 보낼 수 있다. 
                rabbitmq.Bind("mirero-device-update");
            }
        }
    }
}