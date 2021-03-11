using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.Pipeline.Filters;
using Sample.Contracts;

namespace Sample.Components.Consumers
{
    public class FulfillOrderConsumerDefinition : ConsumerDefinition<FulfillOrderConsumer>
    {
        public FulfillOrderConsumerDefinition()
        {
            ConcurrentMessageLimit = 4;
            
        }
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<FulfillOrderConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r =>
            {
                // 1초 간격으로 삼세번..
                r.Interval(3, 1000);
                
                // 다시 해봤자 실패할 것으로 예상되는 예외들에 대해서는 Retry 하지 않게 한다. 
                r.Ignore<InvalidOperationException>();
            });
            
            // 아래처럼 하면, `fulfill-order_error` queue(이 Consumer의 Error Queue)  로 fault 메시지가 이동되지 않음.
            // --> 즉 해당 Queue는 더이상 필요없어짐.
            // endpointConfigurator.DiscardFaultedMessages();
            
            // Filter에 대해 배우면, 아래 같은 것을 할 수 있다고 함.
            //endpointConfigurator.UseFilter(new SpecialFulfillOrderFilter());
        }
    }

}