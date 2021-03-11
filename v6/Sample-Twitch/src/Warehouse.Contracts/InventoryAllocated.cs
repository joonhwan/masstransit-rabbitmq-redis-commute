namespace Warehouse.Contracts
{
    using System;


    public interface InventoryAllocated
    {
        Guid AllocationId { get; }
        
        string ItemNumber { get; }
        decimal Quantity { get; }
    }
}