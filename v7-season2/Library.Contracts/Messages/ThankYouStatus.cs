using System;

namespace Library.Contracts.Messages
{
    public interface ThankYouStatus
    {
        Guid MemberId { get; }
        Guid BookId { get; }
        string Status { get; }
    }
}