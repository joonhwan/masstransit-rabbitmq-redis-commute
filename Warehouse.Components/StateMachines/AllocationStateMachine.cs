using System;
using Automatonymous;
using MassTransit;
using Warehouse.Contracts;

namespace Warehouse.Components.StateMachines
{
    public class AllocationStateMachine : MassTransitStateMachine<AllocationState>
    {
        public AllocationStateMachine()
        {
            Event(() => AllocationCreated, x => x.CorrelateById(m => m.Message.AllocationId));
            
            Schedule(() => HoldExpiration, x => x.HoldDurationToken, s =>
            {
                s.Delay = TimeSpan.FromMinutes(5); // was FromHours(1)
            });

            Initially(
                When(AllocationCreated)
                    .Then(context => { })
                    .Schedule(HoldExpiration, context => context.Init<AllocationHoldDurationExpired>(new
                    {
                        AllocationId = context.Data.AllocationId
                    }))
                    .TransitionTo(Allocated)
            );

            During(Allocated,
                When(HoldExpiration.Received)
                    //.TransitionTo(Released)
                    .Finalize() // .TransitionTo(Final)
                
            );
            
            // Finalize 된 경우(Final State로 전이되는경우)에, 저장소에서 Saga 를 완전히 지운다
            SetCompletedWhenFinalized();
        }
        
        public Schedule<AllocationState, AllocationHoldDurationExpired> HoldExpiration { get; set; }
        
        public State Allocated { get; set; }
        public Event<AllocationCreated> AllocationCreated { get; set; }
    }

    public class AllocationState : SagaStateMachineInstance,
                                   // Saga의 Version 관리는 Backend 별로 상이할 수 있어서 이렇게 된 거 같다.
                                   // 하나의 Saga State가 여러개의 Backend를 지원하려면, 아래처럼 해야 할 듯....
                                   MassTransit.RedisIntegration.IVersionedSaga,
                                   MassTransit.MongoDbIntegration.Saga.IVersionedSaga
    {
        public int Version { get; set; }
        
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public Guid? HoldDurationToken { get; set; }
    }
}