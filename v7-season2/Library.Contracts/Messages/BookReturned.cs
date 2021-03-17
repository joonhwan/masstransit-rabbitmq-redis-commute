using System;

namespace Library.Contracts.Messages
{
    public interface BookReturned
    {
        Guid CheckOutId { get; }
        DateTime Timestamp { get; }
        Guid BookId { get; }
        Guid MemberId { get; }
        DateTime DueDate { get; }
        DateTime ReturnedAt { get; }
    }
    
    public interface ChargeMemberFine
    {
        Guid MemberId { get; }
        decimal Amount { get; }
    }
    
    public interface FineCharged
    {
        Guid MemberId { get; }
        decimal Amount { get; }
    }
    
    public interface FineOverriden
    {
        Guid MemberId { get; }
    }

}