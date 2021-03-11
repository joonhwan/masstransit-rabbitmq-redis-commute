using System;
using System.Threading.Tasks;
using MassTransit;
using Warehouse.Contracts;

namespace Warehouse.Components.Consumers
{
    public class AllocateInventoryConsumer : IConsumer<AllocateInventory>
    {
        public async Task Consume(ConsumeContext<AllocateInventory> context)
        {
            Console.WriteLine("@@ AlocateInventoryConsumer 가 작업을 시작합니다. ");
            //await Task.Delay(5000);
            
            var request = context.Message;

            await context.Publish<AllocationCreated>(new
            {
                AllocationId = request.AllocationId,
                HoldDuration = TimeSpan.FromMinutes(5), //TimeSpan.FromSeconds(8), 
            });

            await context.RespondAsync<InventoryAllocated>(new
            {
                request.AllocationId,
                request.ItemNumber,
                request.Quantity,
            });
            
            Console.WriteLine("@@ AlocateInventoryConsumer 가 작업을 완료했습니다.");
            
        }
    }
}