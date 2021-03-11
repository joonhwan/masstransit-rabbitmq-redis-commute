using System;

namespace Warehouse.Contracts
{
    public interface InventoryAllocated
    {
        Guid AllocationId { get; }
        string ItemNumber { get; }
        decimal Quantity { get; }
    }
}