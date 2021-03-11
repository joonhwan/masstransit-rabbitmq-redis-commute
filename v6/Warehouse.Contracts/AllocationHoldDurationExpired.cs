using System;

namespace Warehouse.Contracts
{
    public interface AllocationHoldDurationExpired
    {
        Guid AllocationId { get; }
    }
}