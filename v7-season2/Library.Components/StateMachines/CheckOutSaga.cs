using System;
using Automatonymous;

namespace Library.Components.StateMachines
{
    public class CheckOutSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        
        public Guid BookId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime DueDate { get; set; }
    }
}