using System;

namespace Sample.Contracts
{
    // fulfill order = "주문 완료"
    public interface FulfillOrder
    {
        Guid OrderId { get;  }
        string CardNumber { get; }
    }
}