using Library.Contracts.Messages;
using MassTransit;
using MassTransit.Topology.Topologies;

namespace Library.Contracts
{
    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            // 만일 특정 속성값을 기준으로 Correlation 하고 싶다면, 이렇게...
            // --> 결국  @use-global-topology-correlated 부분의 코드가 필요 없어진다.
            // 그리고!!!  TODO 주의할 점은 여기서 등록한 CorrelationId 가 Global하지만, 각 StateMachine에서 명시한 Correlation 방법이 더 우선권을 가진다.  
#if USE_MODULE_INITIALIZE
            GlobalTopology.Send.UseCorrelationId<BookAdded>(x => x.BookId);
            GlobalTopology.Send.UseCorrelationId<ReservationRequested>(x => x.ReservationId);
            
            // TODO 메시지 타입별로 주욱 처리해주면 된다.
 #endif
        }
    }
}