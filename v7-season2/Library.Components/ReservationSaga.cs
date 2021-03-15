using System;
using Automatonymous;

namespace Library.Components
{
    public class ReservationSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        
        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
        public Guid? ReservationExpiredScheduleToken { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime ReservedAt { get; set; }
    }
}