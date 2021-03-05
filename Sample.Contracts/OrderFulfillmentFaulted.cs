using System;

namespace Sample.Contracts
{
    public interface OrderFulfillmentFaulted
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; } // Fault 발생시각.
        string FaultReason { get; }
    }
}