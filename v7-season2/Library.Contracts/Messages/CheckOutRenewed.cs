using System;

namespace Library.Contracts.Messages
{
    public interface CheckOutRenewed
    {
        Guid CheckOutId { get; }
        DateTime DueDate { get; }
    }
}