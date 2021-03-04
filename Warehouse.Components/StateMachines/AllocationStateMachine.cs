using System;
using System.Text;
using Automatonymous;
using MassTransit;
using MongoDB.Bson.Serialization.Attributes;
using Warehouse.Contracts;

namespace Warehouse.Components.StateMachines
{
    public class AllocationStateMachine : MassTransitStateMachine<AllocationState>
    {
        public AllocationStateMachine()
        {
            // STEP 1 - 상태 기게의 Event 들을 어떻게 다룰 것인지에 대한 내역.(Event는 이 상태기계 객체의 속성으로 정의된 것이어야 함)
            Event(() => AllocationCreated, x => x.CorrelateById(m => m.Message.AllocationId));
            //      - 상태기계가 사용할 Scheduler의 정의 (2번째 인자는 Cancel Token 같은 역할...이란다.) 
            Schedule(() => HoldExpiration, x => x.HoldDurationToken, s =>
            {
                s.Received = x => x.CorrelateById(m => m.Message.AllocationId); // @scheduled-event-correlation
                s.Delay = TimeSpan.FromSeconds(30); // was FromHours(1)
            });
            
            // STEP 2 - 상태기계의 상태값을 저장할 속성을 지정.
            // 
            // `State` 형으로 정의된 상태값들이 문자열로 저장되는 필드를 지정. 
            // 즉, OrderState 는 `CurrentState` 속성에 저장한다.
            InstanceState(x => x.CurrentState);

            // STEP 3 - 상태기계의 State chart  내역을 기술.
            Initially(
                When(AllocationCreated)
                    .ThenAsync(async context =>
                    {
                        await Console.Out.WriteLineAsync(
                            new StringBuilder()
                                .AppendFormat("Allocation이 생성됨. AllocationId={0}", context.Data.AllocationId)
                                .AppendLine()
                        );
                    })
                    // `AllocationCreated` 이벤트를 받으면, AllocationHoldDurationExpired 이벤트를 schedule 건다
                    .Schedule(HoldExpiration, context => context.Init<AllocationHoldDurationExpired>(new
                    {
                        AllocationId = context.Data.AllocationId // 나중에 Saga를 찾을때 사용할 Id . see @scheduled-event-correlation
                    }))
                    .TransitionTo(Allocated)
            );

            During(Allocated,
                When(HoldExpiration.Received)
                    .ThenAsync(async context =>
                    {
                        await Console.Out.WriteLineAsync(
                            new StringBuilder()
                                .AppendFormat("Allocation 의 재고유지기간 지남. Saga 제거됨. AllocationId={0}", context.Data.AllocationId)
                                .AppendLine()
                            );
                    })
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

        [BsonId]
        public Guid CorrelationId { get; set; }

        // 현재 상태값으로 사용할 문자열.
        public string CurrentState { get; set; }
        
        // AllocationStatemachine 에서 사용할 Scheduler 취소용 토큰?
        public Guid? HoldDurationToken { get; set; }
    }
}