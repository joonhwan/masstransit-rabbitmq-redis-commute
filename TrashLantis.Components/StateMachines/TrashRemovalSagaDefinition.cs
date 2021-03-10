using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;

namespace TrashLantis.Components.StateMachines
{
    public class TrashRemovalSagaDefinition : SagaDefinition<TrashRemovalState>
    {
        public TrashRemovalSagaDefinition()
        {
            //this.ConcurrentMessageLimit = 1; // Debugging 이 쉽게 하려고...
            Endpoint(x =>
            {
                x.ConcurrentMessageLimit = 1;
                //x.PrefetchCount = 1;
                //x.Name = "TrashRemoval"
            });
        }
        
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<TrashRemovalState> sagaConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000, 5000, 30000));
            endpointConfigurator.UseInMemoryOutbox();// 이 시스템의 핵심!
            endpointConfigurator.UseFilter(new CatchMeIfYouCan());
        }
    }

    public class CatchMeIfYouCan : IFilter<ConsumeContext>
    {
        public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            await next.Send(context).ConfigureAwait(false);

            Console.WriteLine("Brake Here");
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}