using System.Threading.Tasks;
using MassTransit;
using Warehouse.Contracts;

namespace Warehouse.Components.Consumers
{
    public class AllocateInventoryConsumer : IConsumer<AllocateInventory>
    {
        public async Task Consume(ConsumeContext<AllocateInventory> context)
        {
            await Task.Delay(500);

            var request = context.Message;
            await context.RespondAsync<InventoryAllocated>(new
            {
                request.AllocationId,
                request.ItemNumber,
                request.Quantity,
            });
            
        }
    }
}