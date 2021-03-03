using System;

namespace Warehouse.Contracts
{
    // (판매등을 위한) 재고(Inventory) 할당을 취소하기 위한 메시지.
    // 원래는 `ReleaseInventoryAllocation` 처럼, 재고 할당을 취소하는 "Command" 였는데, 
    // 이 메시지를 보내는 쪽(Courier Activity)이 누구에게 명령을 Send 해야 할지 알 수 없는 상황인듯하여,
    // `AllocationReleaseRequested` 라는 "Event" 로 바꾸어 Publish() 함. 
    // --> Event 라는 것이 항상 "일을 수행한 결과에 대한 상황 통보"의 역할 뿐 아니라,
    //     Command 처럼 어떤 작업의 요청을 할 수 있음을 깨달음.
    public interface AllocationReleaseRequested
    {
        Guid AllocationId { get; }
        string Reason { get; }
    }
}