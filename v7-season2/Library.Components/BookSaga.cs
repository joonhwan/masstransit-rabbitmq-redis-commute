using System;
using Automatonymous;

namespace Library.Components
{
    public class BookSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } 
            
        public DateTime AddedAt { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
    }
}