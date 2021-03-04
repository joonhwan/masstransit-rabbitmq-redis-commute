using System;
using System.Text;
using System.Threading.Tasks;
using Automatonymous;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using Warehouse.Contracts;

namespace Warehouse.Components.StateMachines
{
    public class AllocationStateMachine : MassTransitStateMachine<AllocationState>
    {
        private readonly ILogger<AllocationStateMachine> _logger;

        public AllocationStateMachine(ILogger<AllocationStateMachine> logger)
        {
            _logger = logger;
            
            // STEP 1 - 상태 기게의 Event 들을 어떻게 다룰 것인지에 대한 내역.(Event는 이 상태기계 객체의 속성으로 정의된 것이어야 함)
            Event(() => AllocationCreated, x => x.CorrelateById(m => m.Message.AllocationId));
            Event(() => AllocationReleaseRequested, x => x.CorrelateById(m => m.Message.AllocationId));
            
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
                    .ThenAsync(context =>
                    {
                        _logger.LogInformation(
                            "Allocation이 생성됨. AllocationId={AllocationId}",
                            context.Data.AllocationId
                        );
                        return Task.CompletedTask;
                    })
                    // `AllocationCreated` 이벤트를 받으면, AllocationHoldDurationExpired 이벤트를 schedule 건다
                    .Schedule(HoldExpiration, context => context.Init<AllocationHoldDurationExpired>(new
                        {
                            // 나중에 Saga를 찾을때 사용할 Id . see @scheduled-event-correlation
                            AllocationId = context.Data.AllocationId
                        }), context => context.Data.HoldDuration)
                    .TransitionTo(Allocated),
                When(AllocationReleaseRequested)
                    .Then(context => _logger.LogInformation("재고가 Allocation되기도 전에 AllocationRelease 됨. AllocationId={AllocationId}",context.Data.AllocationId))
                    .TransitionTo(Released)
            );
            
            During(Allocated,
                When(HoldExpiration.Received)
                    .Then(context => _logger.LogInformation("Allocation 의 재고할당 기간만료됨. Saga 제거됨. AllocationId={AllocationId}",context.Data.AllocationId))
                    //.TransitionTo(Released)
                    .Finalize(), // .TransitionTo(Final),
                When(AllocationReleaseRequested)
                    .Then(context => _logger.LogInformation("Allocation 의 재고할당 해제됨. Saga 제거됨. AllocationId={0}", context.Data.AllocationId))
                    .Unschedule(HoldExpiration)
                    .Finalize()
            );

            During(Released,
                When(AllocationCreated)
                    .Then(context => _logger.LogInformation("Allocation 이 이미 Release 되었어요. 😒"))
                    .Finalize()
            );
            
            // 모든 상태 Enter 시 ... 로그를 함 찍어본다.
            WhenEnterAny(x => x.Then(context =>
            {
                _logger.LogInformation("🚦 {CurrentState} 상태가 됨 : CorrelationId={CorrelationId}", 
                    context.Instance.CurrentState, context.Instance.CorrelationId);
            }));
            
            // Finalize 된 경우(Final State로 전이되는경우)에, 저장소에서 Saga 를 완전히 지운다
            SetCompletedWhenFinalized();
        }
        
        public Schedule<AllocationState, AllocationHoldDurationExpired> HoldExpiration { get; set; }
        
        public State Allocated { get; set; }
        public State Released { get; set; }
        
        public Event<AllocationCreated> AllocationCreated { get; set; }
        public Event<AllocationReleaseRequested> AllocationReleaseRequested { get; set; }
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

    public class AllocationStateMachineDefinition : SagaDefinition<AllocationState>
    {
        public AllocationStateMachineDefinition()
        {
            ConcurrentMessageLimit = 4; // 걍..
        }

        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<AllocationState> sagaConfigurator)
        {
            endpointConfigurator.UseMessageRetry(x => x.Interval(3, TimeSpan.FromSeconds(1)));
            endpointConfigurator.UseInMemoryOutbox(); // 
        }
    }
}