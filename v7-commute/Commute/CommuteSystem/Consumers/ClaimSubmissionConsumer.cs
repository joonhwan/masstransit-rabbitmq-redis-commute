using System;
using System.Threading.Tasks;
using CommuteSystem.Contracts;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;

namespace CommuteSystem.Consumers
{
    public class ClaimSubmissionConsumer : IConsumer<SubmitClaim>
    {
        private readonly ILogger<ClaimSubmissionConsumer> _logger;

        public ClaimSubmissionConsumer(ILogger<ClaimSubmissionConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubmitClaim> context)
        {
            var retryAttempt = context.GetRetryAttempt();
            var message = context.Message;
            _logger.LogInformation("SubmitClaim 처리시작 : {OrderId} (RetryAttempt = {RetryAttempt}", message.OrderId, retryAttempt);

            if (retryAttempt < message.DegreeOfHardness)
            {
                _logger.LogWarning("SubmitClaim 처리중 너무 어려운 Claim을 만났네요. 고심중...(RetryAttempt = {RetryAttempt})", retryAttempt);
                await Task.Delay(10000 * (retryAttempt+1));
                throw new TooHardClaimException($"너무 어려운 Claim. 난이도={message.DegreeOfHardness}");
            }
            
            await Task.Delay(1000);

            _logger.LogInformation("SubmitClaim 처리완료 : {OrderId}", message.OrderId);
        }
    }

    public class TooHardClaimException : Exception
    {
        public TooHardClaimException(string message)
            : base(message)
        {
        }
    }

    public class ClaimSubmissionConsumerDefinition : ConsumerDefinition<ClaimSubmissionConsumer>
    {
        public ClaimSubmissionConsumerDefinition()
        {
            
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ClaimSubmissionConsumer> consumerConfigurator)
        {
            // --- endpoint 수준에서의 retry 
            // endpointConfigurator.UseMessageRetry(r => r.Interval(3, 60_000));
            
            // --- consumer 수준에서의 retry 
            //consumerConfigurator.UseMessageRetry(r => r.Interval(3, 10_000));
            
            // --- retry 할 대상 exception 의 지정
            consumerConfigurator.UseMessageRetry(r =>
            {
                r.Interval(3, 10_000);
                r.Handle<TooHardClaimException>();
            });
        }
    }
}