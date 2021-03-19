using System;
using MassTransit.Topology;

namespace CommuteSystem.Contracts
{
    // @what-is-message-entity-name
    // 이 메시지를 Publish할 경우 수신하게 되는 Exchange의 명칭을 EntityNameAttribute 로 지정가능
    //[EntityName("Mirero.SubmitClaim")]
    public interface SubmitClaim
    {
        Guid CustomerId { get; }
        Guid OrderId { get; }
        string ClaimContents { get; }
        int DegreeOfHardness { get;  }
    }
}