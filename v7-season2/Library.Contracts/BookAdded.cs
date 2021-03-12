using System;
using System.ComponentModel;
    
namespace Library.Contracts
{
    public interface BookAdded
    {
        Guid BookId { get; }
        DateTime Timestamp { get; }
        string Isbn { get; }
        string Title { get; }
    }
}