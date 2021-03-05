using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Courier;
using MassTransit.Courier.Contracts;
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
            
            // Activity 작업 #1 : 재고할당하기. 
            builder.AddActivity("AllocateInventory", new Uri("queue:allocate-inventory_execute"), new
            {
                // OrderId = context.Message.OrderId, // 아래에 Variable 로 전달해봄.
                ItemNumber = "ITEM123",
                Quantity = 10.0m
            });
            
            // ... Activity 를 여러개 생성할 수 있음.
            
            // Activity 작업 #2 : 결재하기.
            builder.AddActivity("Payment", new Uri("queue:payment_execute"), new
            {
                //OrderId = context.Message.OrderId, // 아래 Variable 로 전달된다?!
                Amount = 99.95m,
                //CardNumber = "5999-1234-5000-4321", // 실패 할 경우 (5999 로 시작)
                CardNumber = context.Message.CardNumber,
            });
            
            // 분산처리중 필요한 "변수"를 추가.
            builder.AddVariable("OrderId", context.Message.OrderId);

            await builder.AddSubscription(
                context.SourceAddress,
                RoutingSlipEvents.Faulted,
                RoutingSlipEventContents.None, // 회람쪽지 내역 전체를 보낼 필요는 없다(덩치도 크다고 한다)
                endpoint => endpoint.Send<OrderFulfillmentFaulted>(new
                {
                    OrderId = context.Message.OrderId,
                    Timestamp = InVar.Timestamp,
                    FaultReason ="Fault원인은 ???" // Fault된 Exception 을  어떻게 가져오지???
                })
            );

            await builder.AddSubscription(context.SourceAddress, RoutingSlipEvents.Completed, endpoint =>
            {
                Console.WriteLine("@**@ RoutingSlip DONE ");
                return Task.CompletedTask;
            });

            var routingSlip = builder.Build();

            await context.Execute(routingSlip);
        }
    }
}