using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GreenPipes;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Common.Behaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                    throw new ValidationException(failures);
            }
            return await next();
        }
    }
    
    // Masstransit 의 Validation Middleware
    public class ScopedValidationFilter<TMessage> : IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        private readonly IEnumerable<IValidator<TMessage>> _validators;
        private readonly ILogger<ScopedValidationFilter<TMessage>> _logger;

        public ScopedValidationFilter(IEnumerable<IValidator<TMessage>> validators, ILogger<ScopedValidationFilter<TMessage>> logger )
        {
            _validators = validators;
            _logger = logger;
        }
        
        public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            _logger.LogWarning("** ScopedValidationFilter ** : Validating Message(= {Message} )", context.Message);
            if (_validators.Any())
            {
                var request = context.Message;
                var cancellationToken = context.CancellationToken;
                
                var validationContext = new ValidationContext<TMessage>(request);

                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(validationContext, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                {
                    var errors = string.Join(", ",
                        validationResults.SelectMany(r => r.Errors).Where(f => f != null).Select(f => f.ErrorMessage));
                    _logger.LogError("Validation Failed!!! : Error = {Error}", errors);
                    throw new ValidationException(failures);
                }
            }

            await next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("validation");

        }
    }
}