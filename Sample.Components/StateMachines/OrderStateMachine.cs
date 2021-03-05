using System;
using Automatonymous;
using MassTransit;
using MassTransit.Saga;
using MongoDB.Bson.Serialization.Attributes;
using Sample.Components.StateMachines.OrderStateMachineActivities;
using Sample.Contracts;
using StackExchange.Redis;

namespace Sample.Components.StateMachines
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public OrderStateMachine()
        {
            // STEP 1 - 상태 기게의 Event 들을 어떻게 다룰 것인지에 대한 내역.(Event는 이 상태기계 객체의 속성으로 정의된 것이어야 함)
            Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
            // --> OrderSubmitted 는 OrderStateMachine Saga 가 생성되게 하는 이벤트. 만일, Correlation 한 속성값이 GUID 아니라면, 
            //   https://masstransit-project.com/usage/sagas/automatonymous.html#event-2  에 표시된 것 처럼 x.CorrelateById().SelectId() 구문으로 그냥 GUID 에 해당하는 CorrelationId 값을 하나 만들어야 한다.

            Event(() => OrderAccepted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderFulfillmentFaulted, x => x.CorrelateById(m => m.Message.OrderId));
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
            Event(() => CustomerAccountClosed, x=>
            {
                // CustomerAccountClosed 이벤트의 경우는 `OrderId` 가 없으므로, `CustomerName` 으로 Saga 를 선택한다. 
                x.CorrelateBy((state, context) => state.CustomerNumber == context.Message.CustomerNumber);
            });
            
            // STEP 2 - 상태기계의 상태값을 저장할 속성을 지정.
            // 
            // `State` 형으로 정의된 상태값들이 문자열로 저장되는 필드를 지정. 
            // 즉, OrderState 는 `CurrentState` 속성에 저장한다.
            InstanceState(x => x.CurrentState);
            
            // STEP 3 - 상태기계의 State chart  내역을 기술. 
            
            Initially(
                // Initially() 안에 지정된 메시지는... 이 Saga StateMachine 을 "생성"하는 메시지라고 할 수 있다.
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Instance.SubmitDate = context.Data.Timestamp;
                        context.Instance.CustomerNumber = context.Data.CustomerNumber;
                        context.Instance.Updated = DateTime.UtcNow;
                        context.Instance.PaymentCardNumber = context.Data.PaymentCardNumber;
                    })
                    .TransitionTo(Submitted)
            );
            
            // During(Submitted, Ignore(OrderSubmitted));
            During(Submitted,
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        Console.WriteLine("이미 Submit 되었는데, 왜 또 하는거죠. 😒");
                        context.Instance.SubmitDate ??= context.Data.Timestamp;
                        context.Instance.CustomerNumber ??= context.Data.CustomerNumber;
                        context.Instance.PaymentCardNumber ??= context.Data.PaymentCardNumber;
                    }),
                When(CustomerAccountClosed)
                    .Then(context =>
                    {
                        Console.WriteLine("어어.. 고객이 이탈했네요. 주문 취소합니다.");
                    })
                    .TransitionTo(Cancelled),
                When(OrderAccepted)
                    .Then(x =>
                    {
                        Console.WriteLine("@@@ OrderAccepted 수신됨.");
                    })
                    .Activity(x => x.OfType<AcceptOrderActivity>())
                    .TransitionTo(Accepted)
            );

            During(Accepted,
                When(OrderFulfillmentFaulted)
                    .Then(context =>
                    {
                        //TODO 아직 Fault Reason이 정상적으로 들어오지는 않는다.
                        context.Instance.FaultReason = context.Data.FaultReason;
                    })
                    .TransitionTo(Faulted)
            );

            // `DuringAny` 는 Initial/Final 을 제외한 모든 상태.
            
            // DuringAny(
            //     When(OrderSubmitted)
            //         .Then(context =>
            //         {
            //             context.Instance.SubmitDate ??= context.Data.Timestamp;
            //             context.Instance.CustomerNumber ??= context.Data.CustomerNumber;
            //         })
            // );
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
        public State Accepted { get; private set; }
        public State Cancelled { get; private set; }
        public State Faulted { get; private set; }
        
        public Event<OrderSubmitted> OrderSubmitted { get; private set; }
        public Event<CheckOrder> CheckOrder { get; private set; }
        // @more-saga-1 OrderStateMachine 이 전혀 다른 Event 를 받는 것을 시연. --> 특정 사용자가 탈퇴한 경우.
        public Event<CustomerAccountClosed> CustomerAccountClosed { get; private set; }
        public Event<OrderAccepted> OrderAccepted { get; private set; }
        public Event<OrderFulfillmentFaulted> OrderFulfillmentFaulted { get; private set; }
    }

    public class OrderState 
        : SagaStateMachineInstance
            // Saga의 Version 관리는 Backend 별로 상이할 수 있어서 이렇게 된 거 같다.
            // 하나의 Saga State가 여러개의 Backend를 지원하려면, 아래처럼 해야 할 듯....
        , MassTransit.RedisIntegration.IVersionedSaga
        , MassTransit.MongoDbIntegration.Saga.IVersionedSaga
    {
        // Saga에 새로운 정보가 기록되는 경우 뿐 아니라, 접근만 해도, 이 Version은 +1 되는 것 같다.
        public int Version { get; set; } 

        [BsonId]
        public Guid CorrelationId { get; set; } // ISaga.CorrelationId
        public string CurrentState { get; set; }
        public string CustomerNumber { get; set; }
        public DateTime? SubmitDate { get; set; }
        public DateTime? Updated { get; set; }
        public string FaultReason { get; set; }
        public string PaymentCardNumber { get; set; }
    }
}