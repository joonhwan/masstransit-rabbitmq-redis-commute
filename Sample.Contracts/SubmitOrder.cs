using System;
using System.Diagnostics.CodeAnalysis;
using MassTransit;

// ReSharper disable All

namespace Sample.Contracts
{
    public interface SubmitOrder
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
        string CustomerNumber { get; }   
        string PaymentCardNumber { get; }
        
        MessageData<string> Notes { get; }
    }
}