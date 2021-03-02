using System;

namespace Sample.Contracts
{
    public interface OrderNotFound
    {
        Guid OrderId { get; }
    }
}