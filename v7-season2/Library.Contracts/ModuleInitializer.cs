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
            GlobalTopology.Send.UseCorrelationId<BookAdded>(x => x.BookId);
            // TODO 메시지 타입별로 주욱 처리해주면 된다. 
        }
    }
}