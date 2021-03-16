using System;

namespace Library.Contracts.Messages
{
    public interface NotifyMemberDueDate
    {
        Guid MemberId { get; }
        DateTime DueDate { get; }
    }
}