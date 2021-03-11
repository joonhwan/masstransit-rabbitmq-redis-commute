using System;

namespace Warehouse.Components.CourierActivities
{
    public interface AllocateInventoryLog
    {
        Guid AllocationId { get; }
    }
}