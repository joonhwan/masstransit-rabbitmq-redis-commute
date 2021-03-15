using System;

namespace Library.Contracts.Messages
{
    public interface BookCheckedOut
    {
        Guid BookId { get; }
        DateTime Timestamp { get; }
        Guid MemberId { get; }
    }
}