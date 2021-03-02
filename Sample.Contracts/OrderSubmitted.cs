using System;

namespace Sample.Contracts
{
    public interface OrderSubmitted
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
        string CustomerNumber { get; }
    }
}