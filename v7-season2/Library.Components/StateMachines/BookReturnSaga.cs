using System;
using Automatonymous;

namespace Library.Components.StateMachines
{
    public class BookReturnSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public Guid BookId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime CheckOutAt { get; set; }
        public DateTime ReturnedAt { get; set; }
        public DateTime DueDate { get; set; }
        public Guid? FineRequestId { get; set; }
    }
}