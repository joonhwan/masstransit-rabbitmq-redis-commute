using System;

namespace Library.Contracts.Messages
{
    // Reservation된 다음, 실제로 대출이 일어나지 않고, 일정시간이 지나면
    // `ReservationExpired` 이벤트가 생성된다.
    public interface ReservationExpired
    {
        Guid ReservationId { get; }
    }
}