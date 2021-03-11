using System;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Specifications;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrashLantis.Contracts;

namespace TrashLantis.Components
{
    //
    //  여기서 정의된 Filter는 IBusControl 인터페이스의 GetProbeResult() 를 통해 
    //  볼 수 있다. 
    
    
    public class ConsoleConsumeFilter : IFilter<ConsumeContext>
    {
        public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            Console.WriteLine("@@@@ Consume : MessageId={0}", context.MessageId);
            
            // NOTE! 반드시 next.Send() 를 호출해야 함. !!!
            await next.Send(context);
            // 만약 next.Send() 하지 않으면, `*_skip` queue 로 메시지가 쌓인다.

            //await Task.Delay(0); // dummy
        }

        public void Probe(ProbeContext context)
        {
            // 이 필터에서 사용할 Scope( 일종의 context? ) 를 만든다.
            context.CreateFilterScope("@consoleConsumeFilter"); 
            context.Add("output", "console"); // *ANY*  key-value data.
        }
    }

    public class ConsoleConsumeWithConsumerFilter<TConsumer> : IFilter<ConsumerConsumeContext<TConsumer>> where TConsumer : class
    {
        public async Task Send(ConsumerConsumeContext<TConsumer> context, IPipe<ConsumerConsumeContext<TConsumer>> next)
        {
            Console.WriteLine("@@@@ Consume with Consumer : MessageId={0}, Consumer={1}", context.MessageId, context.Consumer);
            
            // Payload 에는 IServiceProvider 도 있다. 
            var serviceProvider = context.GetPayload<IServiceProvider>();
            var logger = serviceProvider.GetService(typeof(ILogger<ConsoleConsumeWithConsumerFilter<TConsumer>>)) as ILogger;
            logger.LogInformation("Logger를 MS DI 프레임웍에서 제공받아 사용중입니다. ");
                
            await next.Send(context); // NOTE! 반드시 next.Send() 를 호출해야 함. !!! 
        }
        public void Probe(ProbeContext context)
        {
            // 이 필터에서 사용할 Scope( 일종의 context? ) 를 만든다.
            context.CreateFilterScope("@consoleConsumeWithConsumerFilter"); 
            context.Add("output", "console"); // *ANY*  key-value data.
        }
    }
    
    public class ConsoleConsumeWithMessageFilter<TMessage> : IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            Console.WriteLine("@@@@ Consume with Consumer of Typed Message : MessageId={0}, Message={1}",
                context.MessageId,
                // Masstransit의 Reflection Utilityt 함수.. !!!!!
                TypeMetadataCache<TMessage>.ShortName //context.Message
            );
            
            await next.Send(context); // NOTE! 반드시 next.Send() 를 호출해야 함. !!!
        }

        public void Probe(ProbeContext context)
        {
            // 이 필터에서 사용할 Scope( 일종의 context? ) 를 만든다.
            context.CreateFilterScope("@consoleConsumeWithMessageFilter"); 
            context.Add("output", "console"); // *ANY*  key-value data.
        }
    }

    public interface IMessageValidator<in TMessage>
        where TMessage : class
    {
        Task<bool> Validate(ConsumeContext<TMessage> message); 
    }

    public class MessageValidator<TMessage> : IMessageValidator<TMessage> where TMessage : class
    {
        public Task<bool> Validate(ConsumeContext<TMessage> context)
        {
            Console.WriteLine("@@@@ Validated : {0}", context.MessageId);
            return Task.FromResult(true);
        }
    }

    public class ConsoleConsumeWithConsumerAndMessageFilter<TConsumer, TMessage> : IFilter<ConsumerConsumeContext<TConsumer, TMessage>>
        where TConsumer : class
        where TMessage : class
    {
        public async Task Send(ConsumerConsumeContext<TConsumer, TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        {
            Console.WriteLine("@@@@ Consume with Consumer of Typed Message : MessageId={0}, Consumer={1}, Message={2}",
                context.MessageId,
                context.Consumer,
                // Masstransit의 Reflection Utilityt 함수.. !!!!!
                TypeMetadataCache<TMessage>.ShortName //context.Message
            );
            
            var serviceProvider = context.GetPayload<IServiceProvider>();
            var validator = serviceProvider.GetService<IMessageValidator<TMessage>>();
            if (validator != null)
            {
                await validator.Validate(context);
            }
            
            
            await next.Send(context); // NOTE! 반드시 next.Send() 를 호출해야 함. !!!
        }

        public void Probe(ProbeContext context)
        {
            // 이 필터에서 사용할 Scope( 일종의 context? ) 를 만든다.
            context.CreateFilterScope("@consoleConsumeWithConsumerAndMessageFilter"); 
            context.Add("output", "console"); // *ANY*  key-value data.
        }
    }
    
    public class ConsoleConsumeMessageFilterConfigurationObserver : IConsumerConfigurationObserver
    {
        private readonly IConsumePipeConfigurator _configurator;

        public ConsoleConsumeMessageFilterConfigurationObserver(IConsumePipeConfigurator configurator)
        {
            _configurator = configurator;
        }
        
        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator) 
            where TConsumer : class
        {
            // TODO
        }

        public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator) 
            where TConsumer : class 
            where TMessage : class
        {
            // TODO
            var pipeSpec =
                new FilterPipeSpecification<ConsumeContext<TMessage>>(new ConsoleConsumeWithMessageFilter<TMessage>());
            _configurator.AddPipeSpecification(pipeSpec);
        }
    }
}