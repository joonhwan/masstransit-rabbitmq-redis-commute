using System.Threading.Tasks;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace Rabbit.Details
{
    public interface IRabbitMqUsage
    {
        void Configure(IRabbitMqBusFactoryConfigurator cfg);
        Task Test(IBusControl busControl);
    }
}