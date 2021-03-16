using System;

namespace Library.Contracts.Messages
{
    /// <summary>
    /// 언제까지나 기간 연장을 할 수는 없다. 최대 연장 Limit 에 다다른 상태에서 만료 갱신이 요청되면, 이 메시지가 응답된다. 
    /// </summary>
    public interface CheckOutDurationLimitReached
    {
        Guid CheckOutId { get; }
        DateTime DueDate { get; }
    }
}