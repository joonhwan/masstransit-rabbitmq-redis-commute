using System;
using Automatonymous;
using Library.Contracts.Messages;
using MassTransit;

namespace Library.Components.StateMachines
{
    public class ThankYouStateMachine : MassTransitStateMachine<ThankYouSaga>
    {
        public ThankYouStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => BookReserved,
                x => x.CorrelateBy((state, context) =>
                        state.BookId == context.Message.BookId && context.Message.MemberId == state.MemberId)
                    .SelectId(context => context.MessageId ?? NewId.NextGuid())
            );
            Event(() => BookCheckedOut,
                x => x.CorrelateBy((state, context) =>
                        state.BookId == context.Message.BookId && state.MemberId == context.Message.MemberId)
                    .SelectId(context => context.MessageId ?? NewId.NextGuid())
            );

            Initially(
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.ReservationId = context.Data.ReservationId;
                    })
                    .TransitionTo(Active),
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                    })
                    .TransitionTo(Active)
            );

            During(Active,
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.ReservationId = context.Data.ReservationId;
                    }),
                // BookReserved 가 먼저 수신된 상태에서 BookCheckedOut 이 이어서 수신된 경우,
                // 이미 BookId, MemberId, ReservationId 가 다 설정된 상태이므로.. 따로 할게 읍다.
                //
                // 또한, 이벤트를 명시적으로 Ignore() 하면, Masstransit이 해당 메시지를 처리안했다는
                // 경고성 로그가 사라진다.
                Ignore(BookCheckedOut)  
            );

        }
        
        public State Active { get; }
        
        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
    }

    public class ThankYouSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
        public string CurrentState { get; set; }
        public Guid? ReservationId { get; set; }
    }
}