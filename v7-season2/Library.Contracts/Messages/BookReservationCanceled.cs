using System;

namespace Library.Contracts.Messages
{
    public interface BookReservationCanceled
    {
        Guid BookId { get; }
        Guid ReservationId { get; }
    }
}