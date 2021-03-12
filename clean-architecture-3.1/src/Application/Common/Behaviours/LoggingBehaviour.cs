using System;
using CleanArchitecture.Application.Common.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application.Common.Behaviours
{
    public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>
    {
        private readonly ILogger _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public LoggingBehaviour(ILogger<TRequest> logger, ICurrentUserService currentUserService, IIdentityService identityService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _identityService = identityService;
        }

        public async Task Process(TRequest request, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId ?? string.Empty;
            string userName = string.Empty;

            if (!string.IsNullOrEmpty(userId))
            {
                userName = await _identityService.GetUserNameAsync(userId);
            }

            _logger.LogInformation("CleanArchitecture Request: {Name} {@UserId} {@UserName} {@Request}",
                requestName, userId, userName, request);
        }
    }

    // MassTransit 세계에서의 MiddleWare는 "Filter"
    public class LoggingFilter<TMessage> : IFilter<ConsumeContext<TMessage>> 
        where TMessage : class
    {
        public LoggingFilter()
        {
        }
        
        public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            var serviceProvider = context.GetPayload<IServiceProvider>();
            var currentUserService = serviceProvider.GetService<ICurrentUserService>();
            var identityService = serviceProvider.GetService<IIdentityService>();
            var logger = serviceProvider.GetService<ILogger<LoggingFilter<TMessage>>>();
            
            var requestName = typeof(TMessage).Name;
            var userId = currentUserService.UserId ?? string.Empty;
            string userName = string.Empty;

            if (!string.IsNullOrEmpty(userId))
            {
                userName = await identityService.GetUserNameAsync(userId);
            }

            var request = context.Message; // 이게 메시지.
            logger.LogWarning("**MassTransit Filter** CleanArchitecture Request: {Name} {@UserId} {@UserName} {@Request}",
                requestName, userId, userName, request);

            // masstransit의 filter는 반드시 next.Send() 를 호출해야 흐름이 안끊긴다
            // (반대로, 흐름을 끊으려면, 이걸 호출안하면 된다)
            await next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("per-message-logging");
        }
    }

    public class LoggingFilterForConsumerConfigurationObserver : IConsumerConfigurationObserver
    {
        public LoggingFilterForConsumerConfigurationObserver()
        {
        }
        
        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator) where TConsumer : class
        {
        }

        public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator) where TConsumer : class where TMessage : class
        {
            configurator.Message(x => x.UseFilter(new LoggingFilter<TMessage>()));
        }
    }
    
    public class ScopedLoggingFilter<TMessage> : IFilter<ConsumeContext<TMessage>> 
        where TMessage : class
    {
        private readonly ILogger<TMessage> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public ScopedLoggingFilter(ILogger<TMessage> logger, ICurrentUserService currentUserService, IIdentityService identityService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _identityService = identityService;
        }
        
        public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            var requestName = typeof(TMessage).Name;
            var userId = _currentUserService.UserId ?? string.Empty;
            string userName = string.Empty;

            if (!string.IsNullOrEmpty(userId))
            {
                userName = await _identityService.GetUserNameAsync(userId);
            }

            var request = context.Message; // 이게 메시지.
            _logger.LogWarning("**MassTransit ScopedFilter** CleanArchitecture Request: {Name} {@UserId} {@UserName} {@Request}",
                requestName, userId, userName, request);

            // masstransit의 filter는 반드시 next.Send() 를 호출해야 흐름이 안끊긴다
            // (반대로, 흐름을 끊으려면, 이걸 호출안하면 된다)
            await next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("per-message-logging");
        }
    }

    
}
