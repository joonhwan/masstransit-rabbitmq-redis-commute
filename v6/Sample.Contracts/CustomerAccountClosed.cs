using System;

namespace Sample.Contracts
{
    // @more-saga-1 OrderStateMachine 이 전혀 다른 Event 를 받는 것을 시연. --> 특정 사용자가 탈퇴한 경우.
    public interface CustomerAccountClosed
    {
        Guid CustomerId { get; }
        DateTime Timestamp { get; }
        string CustomerNumber { get; }
    }
}