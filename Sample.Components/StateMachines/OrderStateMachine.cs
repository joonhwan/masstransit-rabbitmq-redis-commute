using System;
using Automatonymous;
using MassTransit.RedisIntegration;
using MassTransit;
using MassTransit.Saga;
using Sample.Contracts;
using StackExchange.Redis;

namespace Sample.Components.StateMachines
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public OrderStateMachine()
        {
            Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => CheckOrder,x =>
            {
                x.CorrelateById(m => m.Message.OrderId);
                x.OnMissingInstance(configurator => configurator.ExecuteAsync(async context =>
                {
                    //var responseRequired = context.ResponseAddress != null;
                    var responseRequired = context.RequestId.HasValue; // 요청 메시지가 응답을 요하는 것인지 확인하는 또다른 방법.  
                    if (responseRequired)
                    {
                        await context.RespondAsync<OrderNotFound>(new
                        {
                            context.Message.OrderId
                        });
                    }
                }));
            });
            
            // `State` 형으로 정의된 상태값들이 문자열로 저장되는 필드를 지정. 
            // 즉, OrderState 는 `CurrentState` 속성에 저장한다.
            InstanceState(x => x.CurrentState);
            
            Initially(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Instance.SubmitDate = context.Data.Timestamp;
                        context.Instance.CustomerNumber = context.Data.CustomerNumber;
                        context.Instance.Updated = DateTime.UtcNow;

                    })
                    .TransitionTo(Submitted)
            );
            
            // During(Submitted, Ignore(OrderSubmitted));
            During(Submitted, When(OrderSubmitted).Then(context =>
            {
                Console.WriteLine("이미 Submit 되었는데, 왜 또 하는거죠. 😒");
            }));

            // `DuringAny` 는 Initial/Final 을 제외한 모든 상태.
            DuringAny(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Instance.SubmitDate ??= context.Data.Timestamp;
                        context.Instance.CustomerNumber ??= context.Data.CustomerNumber;

                    })
            );
            DuringAny(
                When(CheckOrder)
                    // 특정 메시지에 대한 응답을 생성할 수 있다.
                    .RespondAsync(context =>
                        context.Init<OrderStatus>(new
                        {
                            OrderId = context.Instance.CorrelationId,
                            State = $"{context.Instance.CurrentState}",
                        })
                    )
            );
        }
        
        public State Submitted { get; private set; }
        
        public Event<OrderSubmitted> OrderSubmitted { get; private set; }
        public Event<CheckOrder> CheckOrder { get; private set; }
    }

    public class OrderState : SagaStateMachineInstance, IVersionedSaga
    {
        public int Version { get; set; }
        
        public Guid CorrelationId { get; set; } // ISaga.CorrelationId
        public string CurrentState { get; set; }
        public string CustomerNumber { get; set; }
        public DateTime? SubmitDate { get; set; }
        public DateTime? Updated { get; set; }

    } 
}