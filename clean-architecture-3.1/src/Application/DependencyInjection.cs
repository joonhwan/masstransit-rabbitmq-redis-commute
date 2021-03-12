using AutoMapper;
using CleanArchitecture.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using CleanArchitecture.Application.WeatherForecasts.Queries.GetWeatherForecasts;
using MassTransit;
using MassTransit.Registration;
using MediatR.Pipeline;

namespace CleanArchitecture.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IRequestPreProcessor<>), typeof(LoggingBehaviour<>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));

            services.AddMediator(configurator =>
            {
                configurator.ConfigureMediator((context, mediatorConfigurator) =>
                {
                    // // Masstransit아,  Consumer가 Configure 되면,
                    // // LoggingFilterForConsumerConfigurationObserver 한테 알려줘.
                    // mediatorConfigurator.ConnectConsumerConfigurationObserver(
                    //     new LoggingFilterForConsumerConfigurationObserver()
                    // );
                    
                    mediatorConfigurator.UseConsumeFilter(typeof(ScopedLoggingFilter<>), context);
                    mediatorConfigurator.UseConsumeFilter(typeof(ScopedValidationFilter<>), context);
                });
                configurator.AddConsumers(Assembly.GetExecutingAssembly());
                configurator.AddRequestClient<GetWeatherForecasts>();
            });
            return services;
        }
    }
}
