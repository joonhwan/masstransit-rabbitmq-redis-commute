using System;

namespace Library.Contracts.Messages
{
    public interface BookCheckedOut
    {
        Guid CheckOutId { get; }
        Guid BookId { get; }
        DateTime Timestamp { get; }
        Guid MemberId { get; }
    }
}