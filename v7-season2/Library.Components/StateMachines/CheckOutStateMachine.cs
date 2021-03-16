using Automatonymous;
using Library.Components.Activities;
using Library.Contracts.Messages;

namespace Library.Components.StateMachines
{
    public class CheckOutStateMachine : MassTransitStateMachine<CheckOutSaga>
    {
        public CheckOutStateMachine(CheckOutSettings settings)
        {
            Event(() => BookCheckedOut, x => x.CorrelateById(m => m.Message.CheckOutId));
            
            InstanceState(saga => saga.CurrentState);

            Initially(
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.CheckOutDate = context.Data.Timestamp;
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.DueDate = context.Instance.CheckOutDate + settings.DefaultCheckOutDuration;
                    })
                    // DI Container 가 지원되도록 NotifyMemberActivity 를 생성/사용하려면. 아래처럼 하면 된다.
                    .Activity(x => x.OfInstanceType<NotifyMemberActivity>())
                    .TransitionTo(CheckedOut)
            );
        }
        public State CheckedOut { get; }
        
        public Event<BookCheckedOut> BookCheckedOut { get; }
    }

    // Automatonymous Activity 임( != Courier Activity)
}