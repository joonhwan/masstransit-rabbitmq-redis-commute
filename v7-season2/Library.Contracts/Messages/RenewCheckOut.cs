using System;

namespace Library.Contracts.Messages
{
    public interface RenewCheckOut
    {
        Guid CheckOutId { get; }
    }
}