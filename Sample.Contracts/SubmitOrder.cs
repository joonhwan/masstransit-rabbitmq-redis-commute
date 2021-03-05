using System;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable All

namespace Sample.Contracts
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public interface SubmitOrder
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
        string CustomerNumber { get; }   
        string PaymentCardNumber { get; }
    }
}