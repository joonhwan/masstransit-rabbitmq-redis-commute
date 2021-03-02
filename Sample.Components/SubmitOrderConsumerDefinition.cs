using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Contracts;
using MassTransit.Definition;
using Sample.Contracts;

namespace Sample.Components
{
    // Consumer로 들어오는 Pipeline을 구성하기 위한 class 
    // 
    // 이전에 
    // - SagaDefinition<TSaga> where TSaga : class, ISaga
    // 로 Saga를 설정했었지? Consumer도 마찬가지로 설정할 수 있어. 
    //
    // - ConsumerDefinition<TConsumer> where TConsumer : class, IConsumer
    public class SubmitOrderConsumerDefinition : ConsumerDefinition<SubmitOrderConsumer>
    {
        public SubmitOrderConsumerDefinition()
        {
            //EndpointName = "주문요청"; // 원래는 "submit-order" 같은거였는데. 맘대로 정해버리면, 고칠데가 많을걸. 하지만, 다른 시스템과 연동하기 위해서라면 필요할 수도
        }
        
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator, // endPoint 는 이걸로 설정 
            IConsumerConfigurator<SubmitOrderConsumer> consumerConfigurator  // consumer pipeline 을 설정.
        )
        {
            // 메시지 처리시  Unhandled Exception 발생하면, Retry 하게 할 수 있다.
            endpointConfigurator.UseMessageRetry(configurator =>
            {
                configurator.Intervals(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));
            });
            
            // pipeline! DEMO! (DEMO 는 DEMO일 뿐 통상 아래같은 코드는 하지 않는다. )
            // 잘 보면 ... pipeline = middleware!
            // 1) 현 Consumer에 대한 Pipeline 구성
            // consumerConfigurator.UseFilter(new RepeatFilter<ConsumerConsumeContext<SubmitOrderConsumer>>());
            consumerConfigurator.UseExecuteAsync(context =>
            {
                Console.WriteLine("@@@ {0} --> {1}", context.SourceAddress, context.DestinationAddress);
                return Task.CompletedTask;
            });
            // 2) 현 Consumer의 특정 메시지에 대한 pipeline 구성 
            consumerConfigurator.Message<SubmitOrder>(configurator =>
            {
                configurator.UseExecuteAsync(context =>
                {
                    Console.WriteLine("@@@@ SubmitOrder 메시지가 왔네요? : {0}.", context.Message);
                    return Task.CompletedTask;
                });
            });

        }
    }
}