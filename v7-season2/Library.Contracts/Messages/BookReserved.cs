using System;

namespace Library.Contracts.Messages
{
    public interface BookReserved
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
        TimeSpan? Duration { get; } // Reserve를 얼마동안 해둘건가.
        Guid MemberId { get; }
        Guid BookId { get; }
    }
}