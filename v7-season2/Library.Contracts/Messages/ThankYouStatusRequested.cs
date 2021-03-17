using System;

namespace Library.Contracts.Messages
{
    public interface ThankYouStatusRequested
    {
        Guid MemberId { get; }
    }
}