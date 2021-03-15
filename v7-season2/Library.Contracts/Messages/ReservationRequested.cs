using System;

namespace Library.Contracts.Messages
{
    public interface ReservationRequested
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
        TimeSpan? Duration { get;  } // Reservation 유지기간.
        Guid MemberId { get; }
        Guid BookId { get; }
    }
}