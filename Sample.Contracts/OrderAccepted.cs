using System;

namespace Sample.Contracts
{
    public interface OrderAccepted
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
        string CardNumber { get; }
    }
}