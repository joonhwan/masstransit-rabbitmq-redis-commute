using System;

namespace Library.Contracts.Messages
{
    public interface BookAddedToMemberCollection
    {
        Guid MemberId { get; }
        Guid BookId { get; }
    }
}