using System.Diagnostics.CodeAnalysis;
using Automatonymous;
using Library.Contracts;
using Library.Contracts.Messages;
using MassTransit;

namespace Library.Components
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    public class BookStateMachine : MassTransitStateMachine<BookSaga>
    {
        public BookStateMachine()
        {
            // @use-global-topology-correlated
            // 만일, BookId 와 일치하는 Saga 가 Repository에 없으면, 새로운 CorrelationId 를 갖는
            // Saga 인스턴스가 하나 만들어진다. 
            Event(() => BookAdded, x => x.CorrelateById(m => m.Message.BookId));
            Event(() => ReservationRequested, x => x.CorrelateById(m => m.Message.BookId));
            
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
                    .PublishAsync(context => context.Init<BookReserved>(new
                    {
                        ReservationId = context.Data.ReservationId,
                        Timestamp = context.Data.Timestamp,
                        MemberId = context.Data.MemberId,
                        BookId = context.Data.BookId 
                    }))
                    .TransitionTo(Reserved));
            
            // DuringAny(
            //     When(BookAdded)
            //         .Then(CopyDataToInstance)
            //         .TransitionTo(Available)
            // );
        }

        private void CopyDataToInstance(BehaviorContext<BookSaga, BookAdded> context)
        {
            var inst = context.Instance;
            var data = context.Data;
            inst.Isbn = data.Isbn;
            inst.Title = data.Title;
            inst.AddedAt = data.Timestamp;
        }

        public Event<BookAdded> BookAdded { get; }
        public Event<ReservationRequested> ReservationRequested { get; }
        
        public State Available { get; }
        public State Reserved { get; }
    }
}