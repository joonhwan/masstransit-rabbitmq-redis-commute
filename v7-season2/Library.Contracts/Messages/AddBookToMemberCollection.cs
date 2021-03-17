using System;

namespace Library.Contracts.Messages
{
    public interface AddBookToMemberCollection
    {
        Guid MemberId { get; }
        Guid BookId { get; }
    }
}