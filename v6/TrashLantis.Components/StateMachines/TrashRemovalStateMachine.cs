using System;
using Automatonymous;
using GreenPipes;
using MassTransit;
using TrashLantis.Contracts;

namespace TrashLantis.Components.StateMachines
{
    public class TrashRemovalStateMachine : MassTransitStateMachine<TrashRemovalState>
    {
        public TrashRemovalStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            
            Event(() => TakeOutTheTrash,
                x =>
                {
                    x.CorrelateBy(instance => instance.BinNumber, context => context.Message.BinNumber);
                    x.SelectId(context => NewId.NextGuid());
                });

            Initially(
                When(TakeOutTheTrash)
                    .Then(x =>
                    {
                        x.Instance.BinNumber = x.Data.BinNumber;
                        x.Instance.RequestTimestamp = x.GetPayload<ConsumeContext>().SentTime ?? DateTime.UtcNow;
                    })
                    .PublishAsync(x => x.Init<EmptyTrashBin>(new { x.Data.BinNumber }))
                    .TransitionTo(Requested)
            );

            During(Requested,
                When(TakeOutTheTrash)
                    .PublishAsync(x => x.Init<EmptyTrashBin>(new { x.Data.BinNumber }))
            );
        }
        
        public State Requested { get; private set; }
        public Event<TakeOutTheTrash> TakeOutTheTrash { get; private set; } 
    }
}