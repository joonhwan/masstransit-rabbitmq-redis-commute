using System;

namespace Library.Contracts.Messages
{
    public interface ReservationRequested
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
        Guid MemberId { get; }
        Guid BookId { get; }
    }
}