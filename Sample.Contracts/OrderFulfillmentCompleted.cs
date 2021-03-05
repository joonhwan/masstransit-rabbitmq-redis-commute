using System;

namespace Sample.Contracts
{
    public interface OrderFulfillmentCompleted
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
    }
}