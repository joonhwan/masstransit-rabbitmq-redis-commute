using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommuteSystem.Consumers;
using CommuteSystem.Contracts;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo04.ErrorHandling
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<HostedServiceConsume>();
                    
                    // SubmitOrder 명령어를 계속 보내는데, 5번에 한번은 오류가 나는 메시지(Amount 의 값이 0)를
                    // Publish 한다. 
                    // --> SubmitOrder의 Consumer인 OrderSubmissionConsumer 가 처리중 예외가 발생하며, 
                    //     이렇게 예외를 발생시킨 메시지는 `OrderSubmission_error` 라는 이름의 Queue에 쌓인다.  
                    services.AddHostedService<HostedServicePublish>();

                    services.AddMassTransit(x =>
                    {
                        // Amount=0 이면 오류가 나는 Consumer 
                        //  오류발생하면, 기본적으로는 Fault<TMessage> 형 메시지를 자동으로 Publish 한다. (물론, 이 메시지를 받아두는 Queue는 없다. 순전히 통지의 용도)
                        //x.AddConsumer<OrderSubmissionConsumer>();
                        
                        // 만일, 오류가 나더라도  Fault<TMessage>를 Publish 안되게 하려면. 아래처럼 하면된다. 
                        //  --> ConsumerDefinition<T> 를 상속 받아서 거기서 설정할 수 있다. 
                        x.AddConsumer<OrderSubmissionConsumer>(typeof(OrderSubmissionConsumerNoFaultErrorPublishDefinition));
                        
                        // SubmitOrder 메시지 처리중 오류가 발생하는 것을 모니터링할 수 있다. 
                        x.AddConsumer<SubmitOrderFaultMonitor>()
                            .Endpoint(e =>
                            {
                                // 모니터링은 모니터링일뿐. 이 Worker Service가 실행중인 동안만 
                                // 유지되는 Endpoint 를 하나 만든다. 
                                e.Temporary = true;
                            });
                        
                        // 모든 오류 메시지를 검출
                        x.AddConsumer<AnyFaultConsumer>()
                            .Endpoint(e => e.Temporary = true);
                        
                        x.UsingRabbitMq((context, configurator) =>
                        {
                            configurator.ConfigureEndpoints(context);
                        });
                    });
                });
    }

    public class OrderSubmissionConsumerNoFaultErrorPublishDefinition : ConsumerDefinition<OrderSubmissionConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<OrderSubmissionConsumer> consumerConfigurator)
        {
            // SubmitOrder 메시지를 처리하다 예외가 발생하더라도 Fault<SubmitOrder> 메시지는 Publish 하지 않는다.
            // 이렇게 해도 Fault<> 메시지만 Publish 안되는 것일 뿐, 오류난 메시지의 처리(`*_error` Queue로 오류 메시지가 옮겨지는 것)는 
            // 여전히 동작한다. 
            endpointConfigurator.PublishFaults = false;
        }
    }

    // 특정 메시지의 처리 오류를 검출하는 경우.. IConsumer<Fault<TMessage>> 를 상속.
    public class SubmitOrderFaultMonitor : IConsumer<Fault<SubmitOrder>>
    {
        private readonly ILogger<SubmitOrderFaultMonitor> _logger;

        public SubmitOrderFaultMonitor(ILogger<SubmitOrderFaultMonitor> logger)
        {
            _logger = logger;
        }
        public Task Consume(ConsumeContext<Fault<SubmitOrder>> context)
        {
            var faultMessage = context.Message;
            var erroredMessage = faultMessage.Message;
            
            _logger.LogWarning("어오..SubmitOrder 처리시 오류발생이 검출되네요...? : Amount={Amount}", erroredMessage.Amount);

            // 해당 메시지가 왜 예외발생했는지도 알 수 있음. 
            var exceptionInfo = string.Join(",", faultMessage.Exceptions.Select(info => $"{info.ExceptionType} : {info.Message} => {info.StackTrace}"));
            
            return Task.CompletedTask;
        }
    }

    // 메시지 유형에 상관없이 모든 오류를 검출하려면, IConsumer<Fault> 를 상속.
    class AnyFaultConsumer : IConsumer<Fault>
    {
        private readonly ILogger<AnyFaultConsumer> _logger;

        public AnyFaultConsumer(ILogger<AnyFaultConsumer> logger)
        {
            _logger = logger;
        }
        public Task Consume(ConsumeContext<Fault> context)
        {
            _logger.LogError("메시지 처리 오류 검출 : {MessageType} / {FirstExceptionMessage}",
                // 오류난 Message 가 몇단계의 상속으로 이루어진 경우, Message Type도 여러개가 들어가는듯. 
                string.Join(",", context.Message.FaultMessageTypes),
                context.Message.Exceptions.First().Message
            );
            return Task.CompletedTask;
        }
    }
}