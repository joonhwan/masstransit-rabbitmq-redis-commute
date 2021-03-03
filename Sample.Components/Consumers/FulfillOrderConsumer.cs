using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Courier;
using Sample.Contracts;

namespace Sample.Components.Consumers
{
    public class FulfillOrderConsumer : IConsumer<FulfillOrder>
    {
        public async Task Consume(ConsumeContext<FulfillOrder> context)
        {
            var trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            
            // Warehouse.Contract.AllocateInventory 형에 대한 아무런 참조가 없이 완전 loose coupled 된 상태로 
            // 연동하기 위해,
            //     - 문자열로 된  Activity 타입명
            //     - 문자열로 된 execute address
            //     - dynamic 객체로 Activity Argument를 구성.
            //
            // "queue:allocate-inventory_execute" ..는  Masstransit 6.x 부터 지원되는 short address
            // (https://masstransit-project.com/usage/producers.html#send) 
            builder.AddActivity("AllocateInventory", new Uri("queue:allocate-inventory_execute"), new
            {
                // OrderId = context.Message.OrderId, // 아래에 Variable 로 전달해봄.
                ItemNumber = "ITEM123",
                Quantity = 10.0m
            });
            // ... Activity 를 여러개 생성할 수 있음. 
            
            // 분산처리중 필요한 "변수"를 추가.
            builder.AddVariable("OrderId", context.Message.OrderId);

            var routingSlip = builder.Build();

            await context.Execute(routingSlip);
        }
    }
}