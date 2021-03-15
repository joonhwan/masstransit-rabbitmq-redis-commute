using System;

namespace Library.Contracts.Messages
{
    public interface BookAdded
    {
        Guid BookId { get; }
        DateTime Timestamp { get; }
        string Isbn { get; }
        string Title { get; }
    }
}