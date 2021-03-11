using GreenPipes;
using MassTransit;
using MassTransit.Definition;

namespace Sample.Components.StateMachines
{
    // 특정 State(여기서는 OrderState) 를 처리하는 [1) Saga, 2)Saga에 대한 endpoint] 설정내역의 정의.
    public class OrderStateMachineDefinition : SagaDefinition<OrderState>
    {
        public OrderStateMachineDefinition()
        {
            this.ConcurrentMessageLimit = 4; // 이 Saga는 prefetch=4 인 Consumer가 된다.  
        }
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<OrderState> sagaConfigurator)
        {
            // @partitioner 
            // NOTE TODO Saga는 Concurrency 문제가 발생할 수 있다. https://masstransit-project.com/usage/sagas/guidance.html  
            //      이런경우 partitioner 라는 개념을 써서, 특정 key 값을 기준으로 들어오는 메시지를 나래비를 세울 수 있다고 한다. 
            //      아래 예는 CustomerNumber 가 동일한 것은 절대 서로 다른 Saga에 동시에 수신되지 않게 된다.??? (맞게 이해한게 맞는지...)
            // var partitioner = endpointConfigurator.CreatePartitioner(8);
            // sagaConfigurator.Message<OrderSubmitted>(configurator => configurator.UsePartitioner(partitioner, context => context.Message.CustomerNumber));
            
            //base.ConfigureSaga(endpointConfigurator, sagaConfigurator);
            endpointConfigurator.UseMessageRetry(configurator => configurator.Intervals(1000, 10_000, 30_000));
            
            // Saga에서, A 라는 메시지 처리중 또 다른 메시지 B 를 Publish/Send 하는 경우, B 에 대한 실제 전송은 A 의 처리로직이 아무런 예외 발생없이 모두 끝난 다음, 전송되게 하고 싶을 때가 있다. 
            // (즉, B 를 전송한 다음, A 메시지 핸들러에서, 먼가 추가 작업을 더 하다가 오류가 발생하는 경우, B 는 전송되지 않는게 맞지 않을까?) 
            // 이런 경우, Outbox 를 설정하면, 메시지 처리 로직에서 Publish/Send 한 B 같은 메시지들은 모두 Buffer 처리 되고 있다가, A 메시지의 핸들링이 완전히 끝난 다음에서야 전송된다.
            // 
            // --> https://masstransit-project.com/usage/exceptions.html#outbox 
            // 
            // 보통은 InMemory Outbox 면 충분하지 않을까... 
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}