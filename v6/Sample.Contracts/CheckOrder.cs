using System;

namespace Sample.Contracts
{
    public interface CheckOrder
    {
        Guid OrderId { get; }
    }
}