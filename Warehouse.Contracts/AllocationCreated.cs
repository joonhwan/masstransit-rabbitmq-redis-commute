using System;

namespace Warehouse.Contracts
{
    public interface AllocationCreated
    {
        Guid AllocationId { get; }
        TimeSpan HoldDuration { get; }
    }
}