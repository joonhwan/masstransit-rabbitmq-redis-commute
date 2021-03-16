using System.Diagnostics.CodeAnalysis;
using Automatonymous;
using Automatonymous.Binders;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Library.Components.StateMachines
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
                    .Then(context =>
                    {
                        // 동시성 처리를 위해 Available 상태에서 처음 받은 ReservationRequested 메시지의 Id 를 적어둔다.
                        context.Instance.ReservationId = context.Data.ReservationId; 
                    })
                    .PublishBookReserved()
                    .TransitionTo(Reserved)
            );

            During(Reserved,
                // 혹시  ReservationRequested 이 중복 전송된 경우...
                When(ReservationRequested)
                    // 여기서 reservationId 가 일치하는 경우에만 BookReserved 를 Publish해야 한다.
                    .IfElse(context => context.Instance.ReservationId == context.Data.ReservationId,
                        binder => binder.PublishBookReserved(),
                        binder => binder.Then(context =>
                        {
                            logger.LogInformation(
                                "@@@ 음. 이 책은 다른 Reservation 으로 이미 Reserved 된 것임. 수신된 ReservationRequested 메시지는 무시됨.");
                        })),
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
                    // TODO 음. Leave Event 에서 처리하는게 더 낫지 않나? see @leave
                    .Then(context => context.Instance.ReservationId = default)
                    .TransitionTo(CheckedOut)
            );
            
            // @leave quantum state-machine 에서는 이런식으로 가이드했는데...
            //
            // WhenLeave(Reserved, _=> _.Then(context =>
            // {
            //     context.Instance.ReservationId = default;
            // }));
            
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

    public static class BookStateMachineExtensions
    {
        public static EventActivityBinder<BookSaga, ReservationRequested> PublishBookReserved(this EventActivityBinder<BookSaga, ReservationRequested> binder)
        {
            return binder.PublishAsync(context => context.Init<BookReserved>(new
            {
                ReservationId = context.Data.ReservationId,
                Timestamp = InVar.Timestamp,
                Duration = context.Data.Duration,
                MemberId = context.Data.MemberId,
                BookId = context.Data.BookId,
            }));
        }
    }
}