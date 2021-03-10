using System;
using Automatonymous;

namespace TrashLantis.Components.StateMachines
{
    public class TrashRemovalState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public string BinNumber { get; set; }
        public DateTime RequestTimestamp { get; set; }
    }
}