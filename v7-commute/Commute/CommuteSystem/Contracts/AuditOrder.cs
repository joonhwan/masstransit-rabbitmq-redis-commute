using System;

namespace CommuteSystem.Contracts
{
    public interface AuditOrder
    {
        Guid OrderId { get; }
    }

    public interface OrderAudited
    {
        Guid OrderId { get; }
    }
}