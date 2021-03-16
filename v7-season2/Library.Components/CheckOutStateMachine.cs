using Automatonymous;
using Library.Contracts.Messages;

namespace Library.Components
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
                        context.Instance.DueDate = context.Instance.CheckOutDate + settings.DefaultCheckOutDuration;
                    })
                    .TransitionTo(CheckedOut)
            );
        }
        public State CheckedOut { get; }
        
        public Event<BookCheckedOut> BookCheckedOut { get; }
    }
}