using System;

namespace CommuteSystem.Contracts
{
    public interface SubmitOrder
    {
        Guid OrderId { get; }
        Guid ProductId { get; }
        Guid CustomerId { get; }
        int Amount { get; }
    }

    
    public interface OrderSubmitted
    {
        Guid OrderId { get; }
        Guid ProductId { get; }
        Guid CustomerId { get; }
    }
}