using System.Diagnostics.CodeAnalysis;
using Automatonymous;
using Library.Contracts;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Library.Components
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    public class BookStateMachine : MassTransitStateMachine<BookSaga>
    {
        public BookStateMachine(ILogger<BookStateMachine> logger)
        {
            // @use-global-topology-correlated
            // 만일, BookId 와 일치하는 Saga 가 Repository에 없으면, 새로운 CorrelationId 를 갖는
            // Saga 인스턴스가 하나 만들어진다. 
            Event(() => BookAdded, x => x.CorrelateById(m => m.Message.BookId));
            Event(() => ReservationRequested, x => x.CorrelateById(m => m.Message.BookId));
            Event(() => BookReservationCanceled, x => x.CorrelateById(m => m.Message.BookId));
            Event(() => BookCheckedOut, x => x.CorrelateById(m => m.Message.BookId));
            
            // 상태값은 문자열로 저장된다(정수형값에 mapping 시킬 수도 있다. )
            InstanceState(x => x.CurrentState);
            // InstanceState(x => x.CurrentState, Available, ... ); // --> 이렇게 하면 정수형값으로 저장(0=None, 1=Initial, 2=Final, 3=Available, ...) 

            Initially(
                When(BookAdded)
                    .Then(CopyDataToInstance)
                    .TransitionTo(Available)
            );

            During(Available,
                When(ReservationRequested)
                    .TransitionTo(Reserved)
                    .PublishAsync(context => context.Init<BookReserved>(new
                    {
                        ReservationId = context.Data.ReservationId,
                        Timestamp = context.Data.Timestamp,
                        MemberId = context.Data.MemberId,
                        BookId = context.Data.BookId,
                        Duration = context.Data.Duration
                    }))
            );

            During(Reserved,
                When(BookReservationCanceled)
                    .Then(context =>
                    {
                        logger.LogInformation("@@@ 책 예약 취소됨.");
                    })
                    .TransitionTo(Available)
            );

            // TODO CHECK ME 여러 상태에서 처리해야 하는 공통사항이 있는경우, 아래처럼 할 수 있다!!
            During(Available, Reserved, //
                When(BookCheckedOut)
                    .TransitionTo(CheckedOut)
            );
            
            WhenEnterAny(binder => binder.Then(context =>
            {
                logger.LogInformation("Book Saga 상태 변경됨 : {State}", context.Instance.CurrentState);
            }));

            // DuringAny(
            //     When(BookAdded)
            //         .Then(CopyDataToInstance)
            //         .TransitionTo(Available)
            // );
        }

        public Event<BookAdded> BookAdded { get; }
        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<BookReservationCanceled> BookReservationCanceled { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
        
        public State Available { get; }
        public State Reserved { get; }
        public State CheckedOut { get; }
        
        private void CopyDataToInstance(BehaviorContext<BookSaga, BookAdded> context)
        {
            var inst = context.Instance;
            var data = context.Data;
            inst.Isbn = data.Isbn;
            inst.Title = data.Title;
            inst.AddedAt = data.Timestamp;
        }

    }
}