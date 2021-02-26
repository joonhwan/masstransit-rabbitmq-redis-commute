using System;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable All

namespace Sample.Contracts
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public interface SubmitOrder
    {
        Guid OrderId { get; }
        DateTime TimeStamp { get; }
        string CustomerNumber { get; }   
    }
}