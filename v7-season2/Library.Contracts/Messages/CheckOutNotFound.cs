using System;

namespace Library.Contracts.Messages
{
    // CheckOut 관련 메시지를 관련 Saga가 없는 상태에서 수신한 경우
    // 사용자 경험을 위해 아래와 같은 메시지를 고안할 수 있다. 
    public interface CheckOutNotFound
    {
        Guid CheckOutId { get; }
    }
}