using System;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Platform.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Sample.Platform.Consumers;

namespace Sample.Platform
{
    public class SampleStartup : IPlatformStartup
    {
        public void ConfigureMassTransit(IServiceCollectionConfigurator configurator, IServiceCollection services)
        {
            configurator.AddConsumer<SampleConsumer>();
        }

        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IRegistrationContext<IServiceProvider> context) where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            // ???
        }

        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IServiceProvider provider) where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            // TODO
            // configurator.ReceiveEndpoint(x =>
            // {
            // });
        }
    }
}