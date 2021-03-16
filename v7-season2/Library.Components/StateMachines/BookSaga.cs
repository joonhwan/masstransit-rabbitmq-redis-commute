using System;
using Automatonymous;

namespace Library.Components.StateMachines
{
    public class BookSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } 
            
        public DateTime AddedAt { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        
        // 한 책을 2 번 Reserve 하려고 할 때의 동시성 처리를...BookSaga가 처리할 수 있도록 하기 위함.
        // 통상 이런 작업은 DB에 의존하려고 할 텐데.... 그러지 못하는 경우라면 도움이 될 수 도 있겠다.  
        public Guid? ReservationId { get; set; }
    }
}