using System;

namespace Library.Contracts.Messages
{
    public interface ReservationCancellationRequested
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
    }
}