using System;

namespace Sample.Contracts
{
    public interface OrderStatus
    {
        Guid OrderId { get; }
        string State { get; }
    }
}